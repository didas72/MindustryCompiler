using System;
using System.Collections.Generic;
using System.Linq;

using MindustryCompiler.Code;

namespace MindustryCompiler
{
    public class Compiler
    {
        private List<string> variables;
        private List<string> functions;
        private List<string> labels;
        private Memory memory;
        private List<string> messages;
        private List<string> switches;
        private List<string> displays;
        private List<string> processors;

        private bool cellsDefined, banksDefined, messagesDefined, switchesDefined, displaysDefined, processorsDefined;
        private bool runOnce, useSwitch;

        private readonly string COMPILE_LOCK;



        public Compiler()
        {
            COMPILE_LOCK = "asd";
        }



        public string Compile(string source)
        {
            Console.WriteLine("Starting compilation...");

            string compiled = string.Empty;
            string outp = string.Empty;

            lock (COMPILE_LOCK)
            {
                Console.WriteLine("Initializing for compilation...");
                InitForCompile();

                string[] sourceLines = source.Split('\n');
                for (int i = 0; i < sourceLines.Length; i++)
                {
                    sourceLines[i] = sourceLines[i].TrimEnd('\r');
                }

                Console.WriteLine("Searching for functions and labels...");
                SearchFunctionsAndLabels(sourceLines);

                Console.WriteLine("Precompiling...");
                string[] precompiled = Precompile(sourceLines);
                Console.WriteLine("Compiling...");
                IInstruction[] instructions = CompileLines(precompiled);

                for (int i = 0; i < instructions.Length; i++)
                {
                    compiled += $"{instructions[i].GetCode()}\n";
                }

                Console.WriteLine("Applying relative jumps...");
                string[] patchedCompiled = SetRelativeJumps(compiled.TrimEnd('\n').Split('\n'));

                Console.WriteLine("Packing code...");
                for (int i = 0; i < patchedCompiled.Length; i++)
                {
                    outp += $"{patchedCompiled[i]}\n";
                }
            }

            return outp;
        }
        private void InitForCompile()
        {
            variables = new List<string>();
            functions = new List<string>();
            labels = new List<string>();
            memory = new Memory();
            messages = new List<string>();
            switches = new List<string>();
            displays = new List<string>();
            processors = new List<string>();
            runOnce = false;
            useSwitch = false;
            memory.stackSize = 32;
        }



        private IInstruction[] CompileLines(string[] lines)
        {
            List<IInstruction> code = new List<IInstruction>();

            if (memory.size == 0)
                throw new OutOfMemoryException("To compile code you need at least one memory cell or bank.");

            memory.AddMapping("_stack_", memory.stackSize);
            code.Add(new ErrStateCheck(memory));

            if (runOnce)
            {
                if (!useSwitch)
                    code.Add(new RunOnce(useSwitch, memory.mapping["$_runOnce_"]));
                else
                {
                    if (switches.Count < 1)
                        throw new MissingDeviceException();

                    code.Add(new RunOnce());
                }
            }

            for (int l = 0; l < lines.Length; l++)
            {
                if (string.IsNullOrEmpty(lines[l])) continue;

                IInstruction instruction = CompileLine(lines[l]);

                if (instruction != null)
                    code.Add(instruction);
            }

            code.Add(new AddressSolveFunc(memory));
            code.Add(new ErrStateSet(memory));

            return code.ToArray();
        }
        private IInstruction CompileLine(string line)
        {
            string instruction = line.Split(' ')[0];
            string[] args = line.Split(' ').Skip(1).ToArray();

            IInstruction inst = CompileOperation(line);

            if (inst != null)
                return inst;
            else
            {
                if (instruction.EndsWith(":"))
                {
                    string lblName = GetLabelName(instruction);

                    return new Label(lblName);
                }

                switch (instruction.ToLowerInvariant())
                {
                    #region Custom

                    #region var
                    case "var": // var <name> || var <name> = <value>
                        if (args.Length == 1) // var <name>
                        {
                            ValidateVariableName(args[0], true);
                            variables.Add(args[0]);
                            return new Variable(args[0]);
                        }
                        else if (args.Length == 3 && args[1] == "=") //var <name> = <value>
                        {
                            ValidateVariableName(args[0], true);
                            variables.Add(args[0]);
                            return new Variable(args[0], args[2]);
                        }
                        else
                            throw new InvalidArgumentStatementException();
                    #endregion

                    #region memvar
                    case "memvar":
                        if (args.Length == 1) //memvar $<memVar>
                        {
                            ValidateMemVariableName(args[0], true);
                            memory.AddMapping(args[0]);
                        }
                        else
                            throw new InvalidArgumentStatementException();
                        break;
                    #endregion

                    #region pointer
                    case "pointer":
                        if (args.Length == 1) //pointer &<pointer>
                        {
                            ValidatePointerName(args[0], true);
                            int index = memory.AddMapping(args[0]);

                            return new Pointer(args[0], index);
                        }
                        else if (args.Length == 3 && args[1].ToLowerInvariant() == "points") //pointer &<pointer> points <memVar>
                        {
                            ValidatePointerName(args[0], true);
                            ValidateMemVariableName(args[2], false);
                            int index = memory.AddMapping(args[0]);
                            int startIndex = memory.mapping[args[2]];

                            return new Pointer(args[0], index, startIndex);
                        }
                        else if (args.Length == 4 && args[1].ToLowerInvariant() == "now" && args[2] == "points") //pointer &<pointer> now points <memVar>
                        {
                            ValidatePointerName(args[0], false);
                            ValidateMemVariableName(args[3], false);
                            int index = memory.AddMapping(args[0]);
                            int newIndex = memory.mapping[args[3]];

                            return new PointerSet(args[0], index, newIndex);
                        }
                        else
                            throw new InvalidArgumentStatementException();
                    #endregion

                    #region call
                    case "call":
                        if (args.Length == 1)
                        {
                            string funcName1 = GetFunctionName(args[0]);

                            return new Call(funcName1, memory);
                        }
                        else
                            throw new InvalidArgumentStatementException();
                    #endregion

                    #region return
                    case "return":
                        if (args.Length == 0)
                        {
                            return new Return();
                        }
                        else
                            throw new InvalidArgumentStatementException();
                    #endregion

                    #region func
                    case "func":
                        if (args.Length == 1)
                        {
                            string funcName = GetFunctionName(args[0]);

                            return new Label(funcName);
                        }
                        else
                            throw new InvalidArgumentStatementException();
                    #endregion

                    #region raw
                    case "raw":
                        if (args.Length == 0)
                            throw new InvalidArgumentStatementException();

                        return new Raw(line);
                    #endregion

                    #endregion

                    #region Default

                    #region jump
                    case "jump":
                        if (args.Length == 1)
                        {
                            ValidateLabelName(args[0], false);

                            return new Jump(args[0], Jump.JumpCondition.always, "\"_null_\"", "\"_null_\"");
                        }
                        else if (args.Length == 4)
                        {
                            ValidateLabelName(args[0], false);

                            return new Jump(args[0], Operators.GetCondition(args[2]), args[1], args[3]);
                        }
                        else
                            throw new InvalidArgumentStatementException();
                    #endregion

                    #region read
                    case "read":
                        if (args.Length == 3 && args[1] == "<=") //read <var> <= $<memVar> || read <var> <= <absIndex>
                        {
                            if (args[2].StartsWith("$")) //read <var> <= $<memVar>
                            {
                                ValidateVariableName(args[0], false);
                                ValidateMemVariableName(args[2], false);

                                return new Read(args[0], memory.mapping[args[2]].ToString());
                            }
                            else //read <var> <= <absIndex>
                            {
                                ValidateVariableName(args[0], false);
                                int index = int.Parse(args[2]);

                                return new Read(args[0], index.ToString());
                            }
                        }
                        break;
                    #endregion

                    #region write
                    case "write":
                        if (args.Length == 3 && args[1] == "=>") //write <var> => $<memVar> (auto memSpace) || write <var> => <absIndex> (auto memSpace)
                        {
                            if (args[2].StartsWith("$")) //write <var> => $<memVar> (auto memSpace)
                            {
                                ValidateVariableName(args[0], false);
                                ValidateMemVariableName(args[2], false);

                                return new Write(args[0], memory.mapping[args[2]].ToString());
                            }
                            else //write <var> => <absIndex> (auto memSpace)
                            {
                                ValidateVariableName(args[0], false);
                                int index = int.Parse(args[2]);

                                return new Write(args[0], index.ToString());
                            }
                        }
                        break;
                    #endregion

                    #region draw
                    case "draw":
                        if (args.Length == 2)
                        {
                            if (args[0].ToLowerInvariant() != "stroke")
                                throw new InvalidArgumentStatementException();

                            return new Draw(Draw.DrawType.stroke, args[1]);
                        }
                        else if (args.Length == 4)
                        {
                            if (args[0].ToLowerInvariant() != "clear")
                                throw new InvalidArgumentStatementException();

                            return new Draw(Draw.DrawType.clear, args[1], args[2], args[3]);
                        }
                        else if (args.Length == 5)
                        {
                            string lower = args[0].ToLowerInvariant();

                            if (lower == "color")
                                return new Draw(Draw.DrawType.color, args[1], args[2], args[3], args[4]);
                            else if (lower == "line")
                                return new Draw(Draw.DrawType.line, args[1], args[2], args[3], args[4]);
                            else if (lower == "rect")
                                return new Draw(Draw.DrawType.rect, args[1], args[2], args[3], args[4]);
                            else if (lower == "lineRect")
                                return new Draw(Draw.DrawType.lineRect, args[1], args[2], args[3], args[4]);
                            else
                                throw new InvalidArgumentStatementException();
                        }
                        else if (args.Length == 6)
                        {
                            string lower = args[0].ToLowerInvariant();

                            if (lower == "poly")
                                return new Draw(Draw.DrawType.poly, args[1], args[2], args[3], args[4], args[5]);
                            else if (lower == "linePoly")
                                return new Draw(Draw.DrawType.linePoly, args[1], args[2], args[3], args[4], args[5]);
                            else
                                throw new InvalidArgumentStatementException();
                        }
                        else if (args.Length == 7)
                        {
                            string lower = args[0].ToLowerInvariant();

                            if (lower == "triangle")
                                return new Draw(Draw.DrawType.triangle, args[1], args[2], args[3], args[4], args[5], args[6]);
                            else if (lower == "image")
                                return new Draw(Draw.DrawType.image, args[1], args[2], args[3], args[4], args[5], args[6]);
                            else
                                throw new InvalidArgumentStatementException();
                        }
                        else
                            throw new InvalidArgumentStatementException();
                    #endregion

                    #region print
                    case "print":
                        if (args.Length >= 1)
                        {
                            return new Print(line.Split(new char[] { ' ' }, 2)[1]);
                        }
                        else
                            throw new InvalidArgumentStatementException();
                    #endregion

                    #region flush
                    case "flush":
                        if (args.Length == 2)
                        {
                            if (args[0].ToLowerInvariant() == "print")
                                return new Flush(true, args[1]);
                            else if (args[0].ToLowerInvariant() == "draw")
                                return new Flush(false, args[1]);
                            else
                                throw new InvalidArgumentStatementException();
                        }
                        else
                            throw new InvalidArgumentStatementException();
                    #endregion

                    #region getlink
                    case "getlink":
                        if (args.Length == 2)
                        {
                            ValidateVariableName(args[0], false);

                            int link = int.Parse(args[1]);

                            return new GetLink(args[0], link);
                        }
                        else
                            throw new InvalidArgumentStatementException();
                    #endregion

                    #region control
                    case "control":
                        if (args.Length == 3)
                        {
                            ValidateVariableName(args[1], false);

                            if (args[0].ToLowerInvariant() == "enabled")
                                return new Control(Control.ControlType.enabled, args[1], args[2]);
                            else if (args[0].ToLowerInvariant() == "configure")
                                return new Control(Control.ControlType.configure, args[1], args[2]);
                            else
                                throw new InvalidArgumentStatementException();
                        }
                        else if (args.Length == 4)
                        {
                            ValidateVariableName(args[1], false);

                            if (args[0].ToLowerInvariant() == "shootp")
                                return new Control(Control.ControlType.shootp, args[1], args[2]);
                            else
                                throw new InvalidArgumentStatementException();
                        }
                        else if (args.Length == 5)
                        {
                            ValidateVariableName(args[1], false);

                            if (args[0].ToLowerInvariant() == "shoot")
                                return new Control(Control.ControlType.shoot, args[1], args[2], args[3]);
                            else if (args[0].ToLowerInvariant() == "color")
                                return new Control(Control.ControlType.color, args[1], args[2], args[3]);
                            else
                                throw new InvalidArgumentStatementException();
                        }
                        else
                            throw new InvalidArgumentStatementException();
                    #endregion

                    #region radar
                    case "radar":
                        if (args.Length == 5)
                        {
                            ValidateVariableName(args[0], false);
                            ValidateVariableName(args[4], false);

                            Radar.Target tg1 = Radar.GetTarget(args[1]);
                            int order = int.Parse(args[2]);
                            Radar.Sort sort = Radar.GetSort(args[3]);

                            return new Radar(args[0], tg1, order, sort, args[4]);
                        }
                        else if (args.Length == 6)
                        {
                            ValidateVariableName(args[0], false);
                            ValidateVariableName(args[5], false);

                            Radar.Target tg1 = Radar.GetTarget(args[1]);
                            Radar.Target tg2 = Radar.GetTarget(args[2]);
                            int order = int.Parse(args[3]);
                            Radar.Sort sort = Radar.GetSort(args[4]);

                            return new Radar(args[0], tg1, tg2, order, sort, args[5]);
                        }
                        else if (args.Length == 7)
                        {
                            ValidateVariableName(args[0], false);
                            ValidateVariableName(args[6], false);

                            Radar.Target tg1 = Radar.GetTarget(args[1]);
                            Radar.Target tg2 = Radar.GetTarget(args[2]);
                            Radar.Target tg3 = Radar.GetTarget(args[3]);
                            int order = int.Parse(args[4]);
                            Radar.Sort sort = Radar.GetSort(args[5]);

                            return new Radar(args[0], tg1, tg2, tg3, order, sort, args[6]);
                        }
                        else
                            throw new InvalidArgumentStatementException();
                    #endregion

                    #region sensor
                    case "sensor":
                        if (args.Length == 3)
                        {
                            ValidateVariableName(args[0], false);
                            ValidateVariableName(args[2], false);
                            if (!args[1].StartsWith("@"))
                                throw new InvalidArgumentStatementException();

                            return new Sensor(args[0], args[1], args[2]);
                        }
                        else
                            throw new InvalidArgumentStatementException();
                    #endregion

                    #region end
                    case "end":
                        if (args.Length == 0)
                            return new End();
                        else
                            throw new InvalidArgumentStatementException();
                    #endregion

                    //Unit Bind/Control/Radar/Locate

                    #endregion

                    default:
                        throw new InvalidCodeStatementException(instruction);
                }
            }

            return null;
        }
        private IInstruction CompileOperation(string line)
        {
            string storeVar = line.Split(' ')[0];
            string[] args = line.Split(' ').Skip(1).ToArray();

            string firstOperand, secondOperand;
            Variable.VarType storeType, firstType, secondType;

            if (IsVariable(storeVar))
                storeType = Variable.VarType.Local;
            else if (IsMemVariable(storeVar))
                storeType = Variable.VarType.Memory;
            else if (IsPointer(storeVar))
                storeType = Variable.VarType.Pointer;
            else
                return null;

            if (args.Length == 2 && args[0] == "=") //is pure assignment
            {
                if (IsMemVariable(args[1]))
                    firstType = Variable.VarType.Memory;
                else if (IsPointer(args[1]))
                    firstType = Variable.VarType.Pointer;
                else if (IsVariable(args[1]))
                    firstType = Variable.VarType.Local;
                else
                    firstType = Variable.VarType.Value;

                secondType = Variable.VarType.None;
                firstOperand = args[1];
                secondOperand = null;
            }
            else if (args.Length == 3 && args[0] == "=") //is one operand op
            {
                if (IsMemVariable(args[2]))
                    firstType = Variable.VarType.Memory;
                else if (IsPointer(args[2]))
                    firstType = Variable.VarType.Pointer;
                else if (IsVariable(args[2]))
                    firstType = Variable.VarType.Local;
                else
                    firstType = Variable.VarType.Value;

                secondType = Variable.VarType.None;
                firstOperand = args[2];
                secondOperand = null;
            }
            else if (args.Length == 4 && args[0] == "=")
            {
                if (IsMemVariable(args[1]))
                    firstType = Variable.VarType.Memory;
                else if (IsPointer(args[1]))
                    firstType = Variable.VarType.Pointer;
                else if (IsVariable(args[1]))
                    firstType = Variable.VarType.Local;
                else
                    firstType = Variable.VarType.Value;

                if (IsMemVariable(args[3]))
                    secondType = Variable.VarType.Memory;
                else if (IsPointer(args[3]))
                    secondType = Variable.VarType.Pointer;
                else if (IsVariable(args[3]))
                    secondType = Variable.VarType.Local;
                else
                    secondType = Variable.VarType.Value;

                firstOperand = args[1];
                secondOperand = args[3];
            }
            else
                return null;

            if (args.Length == 2) //custom set
                return new CustomSet(storeType, storeVar, firstType, firstOperand);
            else //custom op
                return new CustomOperation(storeVar, storeType, firstOperand, firstType, secondOperand, secondType, Operators.GetOperator(args[2]));
        }



        private string[] Precompile(string[] lines)
        {
            List<string> code = new List<string>();

            //setup stack stuff

            for (int l = 0; l < lines.Length; l++)
            {
                if (string.IsNullOrEmpty(lines[l]))
                {
                    code.Add(lines[l]);
                }
                else if (lines[l].StartsWith("#"))
                {
                    code.Add(PrecompileLine(lines[l]));
                }
                else
                {
                    code.Add(lines[l]);
                }
            }

            return code.ToArray();
        }
        private string PrecompileLine(string line)
        {
            string instruction = line.TrimStart('#').Split(' ')[0].ToLowerInvariant();
            string[] args = line.Split(' ').Skip(1).ToArray();

            string ret = string.Empty;

            switch (instruction)
            {
                case "run-once":
                    if (args.Length == 0)
                    {
                        if (!runOnce)
                        {
                            runOnce = true; useSwitch = false;
                            memory.AddMapping("$_runOnce_");
                        }
                    }
                    else throw new InvalidArgumentStatementException();
                    break;

                case "run-once-switch":
                    if (args.Length == 0)
                    {
                        if (!runOnce)
                        {
                            runOnce = true; useSwitch = true;
                        }
                    }
                    else throw new InvalidArgumentStatementException();
                    break;

                case "def-cell":
                    if (args.Length == 1)
                    {
                        if (cellsDefined)
                            throw new DeviceAlreadyDefinedException();

                        int count = int.Parse(args[0]);

                        for (int i = 0; i < count; i++)
                        {
                            memory.AddMemStruct(new MemStruct($"cell{i+1}", memory.size, 64));
                        }

                        cellsDefined = true;
                    }
                    else throw new InvalidArgumentStatementException();
                    break;

                case "def-bank":
                    if (args.Length == 1)
                    {
                        if (banksDefined)
                            throw new DeviceAlreadyDefinedException();

                        int count = int.Parse(args[0]);

                        for (int i = 0; i < count; i++)
                        {
                            memory.AddMemStruct(new MemStruct($"bank{i + 1}", memory.size, 512));
                        }

                        banksDefined = true;
                    }
                    else throw new InvalidArgumentStatementException();
                    break;

                case "def-message":
                    if (args.Length == 1)
                    {
                        if (messagesDefined)
                            throw new DeviceAlreadyDefinedException();

                        int count = int.Parse(args[0]);

                        for (int i = 0; i < count; i++)
                        {
                            messages.Add($"bank{i + 1}");
                        }

                        messagesDefined = true;
                    }
                    else throw new InvalidArgumentStatementException();
                    break;

                case "def-switch":
                    if (args.Length == 1)
                    {
                        if (switchesDefined)
                            throw new DeviceAlreadyDefinedException();

                        int count = int.Parse(args[0]);

                        for (int i = 0; i < count; i++)
                        {
                            switches.Add($"switch{i + 1}");
                        }

                        switchesDefined = true;
                    }
                    else throw new InvalidArgumentStatementException();
                    break;

                case "def-display":
                    if (args.Length == 1)
                    {
                        if (displaysDefined)
                            throw new DeviceAlreadyDefinedException();

                        int count = int.Parse(args[0]);

                        for (int i = 0; i < count; i++)
                        {
                            displays.Add($"display{i + 1}");
                        }

                        displaysDefined = true;
                    }
                    else throw new InvalidArgumentStatementException();
                    break;

                case "def-processor":
                    if (args.Length == 1)
                    {
                        if (processorsDefined)
                            throw new DeviceAlreadyDefinedException();

                        int count = int.Parse(args[0]);

                        for (int i = 0; i < count; i++)
                        {
                            processors.Add($"processor{i + 1}");
                        }

                        processorsDefined = true;
                    }
                    else throw new InvalidArgumentStatementException();
                    break;

                case "stack-size":
                    if (args.Length == 1)
                    {
                        int size = int.Parse(args[0]);

                        memory.stackSize = size;
                    }
                    else throw new InvalidArgumentStatementException();
                    break;

                default:
                    throw new InvalidPreprocessorStatementException();
            }

            return ret;
        }



        private string[] SetRelativeJumps(string[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("<relJump="))
                {
                    Console.WriteLine($"Setting relative jump for '{lines[i]}'.");

                    int startIndex = lines[i].IndexOf("<relJump=");
                    int endIndex = lines[i].IndexOf(">", startIndex);

                    string replacing = lines[i].Substring(startIndex, endIndex - startIndex + 1);

                    int relJump = int.Parse(replacing.Substring(9, endIndex - startIndex - 9));
                    int absJump = i + relJump;

                    lines[i] = lines[i].Replace(replacing, absJump.ToString());
                }
            }

            return lines;
        }



        private void SearchFunctionsAndLabels(string[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrEmpty(lines[i])) continue;

                ValidateFunction(lines[i], i);

                ValidateLabel(lines[i], i);
            }
        }




        private readonly string[] keywords = new string[] { "var", "func", ":", "call", "return", "_returnAddr_", "_runOnce_", "$_runOnce_", "_entryPoint_", "_null_", "_stackPointer_", "_addressSolve_", "_addressMemSpace_", "_addressOffset_", "_addressSolveFunc_", "_addressSolveRet_", "_errState_", "_indexedAddress_", "_tmpAddress_", "_tmpValue_", "_tmpValue1_", "_tmpValue2_", "_tmpValue2_", "_tmpValue3_", "_tmpValue4_", "_tmpValue5_", "_tmpResult_" };

        private void ValidateVariableName(string name, bool newName)
        {
            foreach (string key in keywords)
                if (name == key)
                    throw new InvalidVariableNameException(name + " " + newName);

            if (newName && name.StartsWith("@"))
                throw new InvalidVariableNameException();

            if (variables.Any((string s) => s == name) == newName) 
                throw new InvalidVariableNameException(name + " " + newName);
        }
        private void ValidateFunctionName(string name, bool newName)
        {
            foreach (string key in keywords)
                if (name == key)
                    throw new InvalidFunctionNameException();

            if (functions.Any((string s) => s == name) == newName)
                throw new InvalidFunctionNameException();
        }
        private void ValidateLabelName(string name, bool newName)
        {
            foreach (string key in keywords)
                if (name == key)
                    throw new InvalidLabelNameException();

            if (labels.Any((string s) => s == name) == newName)
                throw new InvalidLabelNameException(name + " " + newName + " " + labels.Count);
        }
        private void ValidateMemVariableName(string name, bool newName)
        {
            if (!name.StartsWith("$"))
                throw new InvalidVariableNameException();

            foreach (string key in keywords)
                if (name == key)
                    throw new InvalidVariableNameException();

            if (memory.mapping.ContainsKey(name) == newName)
                throw new InvalidVariableNameException(name + " " + newName);
        }
        private void ValidatePointerName(string name, bool newName)
        {
            if (!name.StartsWith("&"))
                throw new InvalidVariableNameException();

            foreach (string key in keywords)
                if (name == key)
                    throw new InvalidVariableNameException();

            if (memory.mapping.ContainsKey(name) == newName)
                throw new InvalidVariableNameException();
        }
        private void ValidateFunction(string line, int index)
        {
            string[] parts = line.Split(' ');

            if (parts.Length != 2)
                return;

            if (parts[0] == "func")
            {
                string funcName = parts[1].Substring(0, parts[1].Length - 2);

                ValidateFunctionName(funcName, true);

                functions.Add(funcName);
            }
        }
        private void ValidateLabel(string line, int index)
        {
            string[] parts = line.Split(' ');

            if (parts.Length != 1)
                return;

            if (parts[0].EndsWith(":"))
            {
                if (parts.Length == 1)
                {
                    string lblName = parts[0].Substring(0, parts[0].Length - 1);

                    ValidateLabelName(lblName, true);

                    labels.Add(lblName);
                }
            }
        }
        private bool IsVariable(string name)
        {
            try { ValidateVariableName(name, false); } catch { return false; }
            return true;
        }
        private bool IsFunction(string name)
        {
            try { ValidateFunctionName(name, false); } catch { return false; }
            return true;
        }
        private bool IsLabel(string name)
        {
            try { ValidateLabelName(name, false); } catch { return false; }
            return true;
        }
        private bool IsMemVariable(string name)
        {
            try { ValidateMemVariableName(name, false); } catch { return false; }
            return true;
        }
        private bool IsPointer(string name)
        {
            try { ValidatePointerName(name, false); } catch { return false; }
            return true;
        }
        private string GetFunctionName(string line)
        {
            string ret;

            if (line.EndsWith("()"))
            {
                ret = line.Substring(0, line.Length - 2);
                ValidateFunctionName(ret, false);

                return ret;
            }

            throw new InvalidFunctionNameException(line);
        }
        private string GetLabelName(string line)
        {
            string ret;

            if (line.EndsWith(":"))
            {
                ret = line.Substring(0, line.Length - 1);
                ValidateLabelName(ret, false);

                return ret;
            }

            throw new InvalidLabelNameException(line);
        }
    }
}
