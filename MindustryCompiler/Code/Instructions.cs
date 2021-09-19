using System.Collections.Generic;

namespace MindustryCompiler.Code
{
    #region Custom
    public class Call : IInstruction
    {
        public string funcName;
        public Memory memoryRef;

        public Call(string funcName, Memory memoryRef) { this.funcName = funcName; this.memoryRef = memoryRef; }

        //public string GetCode() => $"op add _returnAddr_ @counter 2\njump {funcName} always";

        public string GetCode() => $"\top add _returnAddress_ @counter 9\n{new Push("_returnAddress_", memoryRef).GetCode()}\n\tjump {funcName} always";
    }

    public class Return : IInstruction
    {
        public Return() {  }

        //public string GetCode() => "set @counter _returnAddr_";
        public string GetCode() => $"\t{new Pop("_returnAddress_").GetCode()}\n\tset @counter _returnAddress_";
    }

    public class Label : IInstruction
    {
        public string lblName;

        public Label(string lblName) { this.lblName = lblName; }

        public string GetCode() => $"{lblName}:";
    }

    public class RunOnce : IInstruction
    {
        public bool useSwitch;
        public int index;

        public RunOnce() { this.useSwitch = true; this.index = 0; }
        public RunOnce(bool useSwitch, int index) { this.useSwitch = useSwitch; this.index = index; }

        public string GetCode()
        {
            if (useSwitch)
                return "sensor _runOnce_ switch1 @enabled\njump <relJump=2> equal _runOnce_ false\nend\ncontrol enabled switch1 true 0 0 0\n_entryPoint_:";
            else
                return $"{new Read("_runOnce_", index.ToString()).GetCode()}\njump _entryPoint_ equal _runOnce_ 0\nend\n_entryPoint_:\n{new Write("1", index.ToString()).GetCode()}";
        }
         
    }

    public class Variable : IInstruction
    {
        public string name;
        public string startValue;

        public Variable(string name)
        {
            this.name = name; this.startValue = "0";
        }
        public Variable(string name, string startValue)
        {
            this.name = name; this.startValue = startValue;
        }

        public string GetCode() => $"set {name} {startValue}";

        public enum VarType
        {
            None,
            Local,
            Memory,
            Pointer,
            Value,
        }
    }

    public class Push : IInstruction
    {
        public string variable;
        public Memory memoryRef;

        public Push(string variable, Memory memoryRef) { this.variable = variable; this.memoryRef = memoryRef; }

        public string GetCode() => $"{new Write(variable, "_stackPointer_").GetCode()}\nop add _stackPointer_ _stackPointer_ 1\njump <relJump=2> lessThan _stackPointer_ {memoryRef.stackSize}\njump _errState_ always";
    }

    public class Pop : IInstruction
    {
        public string variable;

        public Pop(string variable) { this.variable = variable; }

        public string GetCode() => $"op sub _stackPointer_ _stackPointer_ 1\njump <relJump=2> greaterThanEq _stackPointer_ 0\njump _errState_ always\n{new Read(variable, "_stackPointer_").GetCode()}";
    }

    public class Raw : IInstruction
    {
        public string inst;

        public Raw(string inst) { this.inst = inst; }

        public string GetCode() => $"{inst}\n";
    }

    public class Pointer : IInstruction
    {
        public string name;
        public string startIndex;
        public string index;

        public Pointer(string name, int index) { this.name = name; this.startIndex = "\"_null_\""; this.index = index.ToString(); }
        public Pointer(string name, int index, int startIndex) { this.name = name; this.startIndex = startIndex.ToString(); this.index = index.ToString(); }

        public string GetCode() => new Write(startIndex, index).GetCode();
    }

    public class PointerSet : IInstruction
    {
        public string name;
        public string newIndex;
        public string index;

        public PointerSet(string name, int index, int newIndex) { this.name = name; this.newIndex = newIndex.ToString(); this.index = index.ToString(); }

        public string GetCode() => new Write(newIndex, index).GetCode();
    }

    public class IndexedAddress : IInstruction
    {
        public string baseAddress;
        public string offsetVariable;

        public IndexedAddress(string baseAddress, string offsetVariable) { this.baseAddress = baseAddress; this.offsetVariable = offsetVariable; }

        public string GetCode() => $"op add _indexedAddress_ {baseAddress} {offsetVariable}";
    }

    public class AddressSolve : IInstruction
    {
        public AddressSolve() { }

        public string GetCode() => $"op add _addressSolveRet_ @counter 2\njump _addressSolveFunc_ always";
    }

    public class AddressSolveFunc : IInstruction
    {
        public Memory memoryRef;

        public AddressSolveFunc(Memory memoryRef) { this.memoryRef = memoryRef; }

        public string GetCode()
        {
            string outp = "_addressSolveFunc_:\n";
            outp += $"jump _errState_ greaterThanEq _addressSolve_ {memoryRef.size}\n";

            foreach (KeyValuePair<string, (int, int)> memStruct in memoryRef.GetStructsBaseTop())
            {
                outp += $"jump <relJump=4> greaterThanEq _addressSolve_ {memStruct.Value.Item2}\n";
                outp += $"set _addressMemSpace_ {memStruct.Key}\n";
                outp += $"op sub _addressOffset_ _addressSolve_ {memStruct.Value.Item1}\n";
                outp += "set @counter _addressSolveRet_\n";
            }

            outp += "jump _errState_ always";

            return outp;
        }
    }

    public class ErrStateCheck : IInstruction
    {
        public Memory memoryRef;

        public ErrStateCheck(Memory memoryRef) { this.memoryRef = memoryRef; }

        //err var is used but not locked in compiler bc it is just at start and wont be used again
        public string GetCode() => $"{new Read("err", "0").GetCode()}\njump _errState_ equal err \"ERR\"";
    }

    public class ErrStateSet : IInstruction
    {
        public Memory memoryRef;

        public ErrStateSet(Memory memoryRef) { this.memoryRef = memoryRef; }

        public string GetCode() => $"_errState_:\nset _stackPointer_ {memoryRef.stackSize}\n{new Write("\"ERR\"", "0").GetCode()}\nend";
    }

    public class CustomSet : IInstruction
    {
        public Variable.VarType storeType;
        public string store;
        public Variable.VarType sourceType;
        public string source;

        public CustomSet(Variable.VarType storeType, string store, Variable.VarType sourceType, string source) { this.storeType = storeType; this.store = store; this.sourceType = sourceType; this.source = source; }

        public string GetCode()
        {
            string valueGet, valueSave, valueStore;

            switch (storeType)
            {
                case Variable.VarType.Local:
                    valueSave = store;
                    valueStore = string.Empty;
                    break;

                case Variable.VarType.Memory:
                    valueSave = "_tmpValue_";
                    valueStore = $"{new Write("_tmpValue_", store).GetCode()}";
                    break;

                case Variable.VarType.Pointer:
                    valueSave = "_tmpValue_";
                    valueStore = $"{new Read("_tmpAddress_", store).GetCode()}\n{new Write("_tmpValue_", "_tmpAddress_").GetCode()}";
                    break;

                default:
                    throw new System.Exception();
            }

            switch (sourceType)
            {
                case Variable.VarType.Local:
                case Variable.VarType.Value:
                    valueGet = $"{new Set(valueSave, source).GetCode()}\n";
                    break;

                case Variable.VarType.Memory:
                    valueGet = $"{new Read(valueSave, source).GetCode()}\n";
                    break;

                case Variable.VarType.Pointer:
                    valueGet = $"{new Read("_tmpAddress_", source).GetCode()}\n{new Read(valueSave, "_tmpAddress_").GetCode()}\n";
                    break;

                default:
                    throw new System.Exception();
            }

            return $"{valueGet}{valueStore}";
        }
    }

    public class CustomOperation : IInstruction
    {
        public string store, firstOperand, secondOperand;
        public Variable.VarType storeType, firstType, secondType;
        public Operation.Op op;

        public CustomOperation(string store, Variable.VarType storeType, string firstOperand, Variable.VarType firstType, string secondOperand, Variable.VarType secondType, Operation.Op op) { this.store = store; this.storeType = storeType; this.firstOperand = firstOperand; this.firstType = firstType; this.secondOperand = secondOperand; this.secondType = secondType; this.op = op; }

        public string GetCode()
        {
            string op1Get, op2Get, opDo, opStore, resStore;

            switch (storeType)
            {
                case Variable.VarType.Local:
                    opStore = store;
                    resStore = string.Empty;
                    break;

                case Variable.VarType.Memory:
                    opStore = "_tmpResult_";
                    resStore = $"{new Write("_tmpResult_", store).GetCode()}";
                    break;

                case Variable.VarType.Pointer:
                    opStore = "_tmpResult_";
                    resStore = $"{new Read("_tmpAddress_", store).GetCode()}\n{new Write("_tmpResult_", "_tmpAddress_").GetCode()}";
                    break;

                default:
                    throw new System.Exception();
            }

            switch (firstType)
            {
                case Variable.VarType.Local:
                case Variable.VarType.Value:
                    op1Get = $"{new Set("_tmpValue_", firstOperand).GetCode()}\n";
                    break;

                case Variable.VarType.Memory:
                    op1Get = $"{new Read("_tmpValue_", firstOperand).GetCode()}\n";
                    break;

                case Variable.VarType.Pointer:
                    op1Get = $"{new Read("_tmpAddress_", firstOperand).GetCode()}\n{new Read("_tmpValue_", "_tmpAddress_").GetCode()}\n";
                    break;

                default:
                    throw new System.Exception();
            }

            switch (secondType)
            {
                case Variable.VarType.Local:
                case Variable.VarType.Value:
                    op2Get = $"{new Set("_tmpValue1_", secondOperand).GetCode()}\n";
                    break;

                case Variable.VarType.Memory:
                    op2Get = $"{new Read("_tmpValue1_", secondOperand).GetCode()}\n";
                    break;

                case Variable.VarType.Pointer:
                    op2Get = $"{new Read("_tmpAddress_", secondOperand).GetCode()}\n{new Read("_tmpValue1_", "_tmpAddress_").GetCode()}\n";
                    break;

                default:
                    throw new System.Exception();
            }

            opDo = secondType == Variable.VarType.None ? $"{new Operation(opStore, firstOperand, op).GetCode()}" : $"{new Operation(opStore, firstOperand, secondOperand, op).GetCode()}";

            return $"{op1Get}{op2Get}{opDo}{resStore}";
        }
    }
    #endregion



    #region Default
    public class Jump : IInstruction
    {
        public string lblName;
        public JumpCondition condition;
        public string v1, v2;

        public Jump(string lblName, JumpCondition condition, string v1, string v2) { this.lblName = lblName; this.condition = condition; this.v1 = v1; this.v2 = v2; }

        public string GetCode() => $"jump {lblName} {condition} {v1} {v2}";

        public enum JumpCondition
        {
            always,
            equal,
            lessThan,
            lessThanEq,
            greaterThan,
            greaterThanEq,
            notEqual,
            strictEqual,
        }
    }

    public class Operation : IInstruction
    {
        public string store, operand1, operand2;
        public Op op;

        public Operation(string store, string operand, Op op) { this.store = store;  this.operand1 = operand; this.op = op; operand2 = "\"_null_\""; }
        public Operation(string store, string operand1, string operand2, Op op) { this.store = store; this.operand1 = operand1; this.operand2 = operand2; this.op = op; }

        public string GetCode() => $"op {op} {store} {operand1} {operand2}";
        
        public enum Op
        {
            add,
            sub,
            mul,
            div,
            idiv,
            mod,
            pow,
            equal,
            notEqual,
            land,
            lessThan,
            lessThanEq,
            greaterThan,
            greaterThanEq,
            strictEqual,
            shl,
            shr,
            or,
            and,
            xor,
            not,
            max,
            min,
            angle,
            len,
            noise,
            abs,
            log,
            log10,
            sin,
            cos,
            tan,
            floor,
            ceil,
            sqrt,
            rand,
        }
    }

    public class Set : IInstruction
    {
        public string variable, value;

        public Set(string variable, string value) { this.variable = variable;  this.value = value; }

        public string GetCode() => $"set {variable} {value}";
    }

    public class Read : IInstruction
    {
        public string store;
        public string address;

        public Read(string store, string address) { this.store = store; this.address = address; }

        public string GetCode() => $"set _addressSolve_ {address}\n{new AddressSolve().GetCode()}\nread {store} _addressMemSpace_ _addressOffset_";
    }

    public class Write : IInstruction
    {
        public string value;
        public string address;

        public Write(string value, string address) { this.value = value; this.address = address; }

        public string GetCode() => $"set _addressSolve_ {address}\n{new AddressSolve().GetCode()}\nwrite {value} _addressMemSpace_ _addressOffset_";
    }

    public class Draw : IInstruction
    {
        public string v1, v2, v3, v4, v5, v6;
        public DrawType type;

        public Draw(DrawType type, string v1) { this.type = type; this.v1 = v1; this.v2 = "0"; this.v3 = "0"; this.v4 = "0"; this.v5 = "0"; this.v6 = "0"; }
        public Draw(DrawType type, string v1, string v2, string v3) { this.type = type; this.v1 = v1; this.v2 = v2; this.v3 = v3; this.v4 = "0"; this.v5 = "0"; this.v6 = "0"; }
        public Draw(DrawType type, string v1, string v2, string v3, string v4) { this.type = type; this.v1 = v1; this.v2 = v2; this.v3 = v3; this.v4 = v4; this.v5 = "0"; this.v6 = "0"; }
        public Draw(DrawType type, string v1, string v2, string v3, string v4, string v5) { this.type = type; this.v1 = v1; this.v2 = v2; this.v3 = v3; this.v4 = v4; this.v5 = v5; this.v6 = "0"; }
        public Draw(DrawType type, string v1, string v2, string v3, string v4, string v5, string v6) { this.type = type; this.v1 = v1; this.v2 = v2; this.v3 = v3; this.v4 = v4; this.v5 = v5; this.v6 = v6; }

        public string GetCode() => $"draw {type} {v1} {v2} {v3} {v4} {v5} {v6}";

        public enum DrawType
        {
            clear,
            color,
            stroke,
            line,
            rect,
            lineRect,
            poly,
            linePoly,
            triangle,
            image,
        }
    }

    public class Print : IInstruction
    {
        public string content;

        public Print(string content) { this.content = content; }

        public string GetCode() => $"print {content}";
    }

    public class Flush : IInstruction
    {
        public bool isPrint;
        public string dest;

        public Flush(bool isPrint, string dest) { this.isPrint = isPrint; this.dest = dest; }

        public string GetCode()
        {
            if (isPrint)
                return $"printflush {dest}";
            else
                return $"drawflush {dest}";
        }
    }

    public class End : IInstruction
    {
        public End() { }

        public string GetCode() => "end";
    }

    public class GetLink : IInstruction
    {
        public string variable;
        public int link;

        public GetLink(string variable, int link) { this.variable = variable; this.link = link; }

        public string GetCode() => $"getlink {variable} {link}";
    }

    public class Control : IInstruction
    {
        public ControlType controlType;
        public string v1, v2, v3, v4;

        public Control(ControlType controlType, string v1, string v2) { this.controlType = controlType; this.v1 = v1; this.v2 = v2; this.v3 = "0"; this.v4 = "0"; }
        public Control(ControlType controlType, string v1, string v2, string v3) { this.controlType = controlType; this.v1 = v1; this.v2 = v2; this.v3 = v3; this.v4 = "0"; }
        public Control(ControlType controlType, string v1, string v2, string v3, string v4) { this.controlType = controlType; this.v1 = v1; this.v2 = v2; this.v3 = v3; this.v4 = v4; }

        public string GetCode() => $"control {controlType} {v1} {v2} {v3} {v4}";

        public enum ControlType
        {
            enabled,
            shoot,
            shootp,
            configure,
            color,
        }
    }
    
    public class Radar : IInstruction
    {
        public string from;
        public Target tg1, tg2, tg3;
        public int order;
        public Sort sort;
        public string store;

        public Radar(string from, Target tg1, int order, Sort sort, string store) { this.from = from; this.tg1 = tg1; this.tg2 = Target.any; this.tg3 = Target.any; this.order = order; this.sort = sort; this.store = store; }
        public Radar(string from, Target tg1, Target tg2, int order, Sort sort, string store) { this.from = from; this.tg1 = tg1; this.tg2 = tg2; this.tg3 = Target.any; this.order = order; this.sort = sort; this.store = store; }
        public Radar(string from, Target tg1, Target tg2, Target tg3, int order, Sort sort, string store) { this.from = from; this.tg1 = tg1; this.tg2 = tg2; this.tg3 = tg3; this.order = order; this.sort = sort; this.store = store; }

        public string GetCode() => $"radar {tg1} {tg2} {tg3} {sort} {from} {order} {store}";

        public enum Target
        {
            any,
            enemy,
            ally,
            player,
            attacker,
            flying,
            boss,
            ground,
        }
        public enum Sort
        {
            distance,
            health,
            shield,
            armor,
            maxHealth,
        }

        public static Target GetTarget(string target)
        {
            switch (target.ToLowerInvariant())
            {
                case "any": return Target.any;
                case "enemy": return Target.enemy;
                case "ally": return Target.ally;
                case "player": return Target.player;
                case "attacker": return Target.attacker;
                case "flying": return Target.flying;
                case "boss": return Target.boss;
                case "ground": return Target.ground;

                default:
                    throw new InvalidOperatorException();
            }
        }
        public static Sort GetSort(string sort)
        {
            switch (sort.ToLowerInvariant())
            {
                case "distance": return Sort.distance;
                case "health": return Sort.health;
                case "shield": return Sort.shield;
                case "armor": return Sort.armor;
                case "maxhealth": return Sort.maxHealth;

                default:
                    throw new InvalidOperatorException();
            }
        }
    }

    public class Sensor : IInstruction
    {
        public string store;
        public string type;
        public string inside;

        public Sensor(string store, string type, string inside) { this.store = store; this.type = type; this.inside = inside; }

        public string GetCode() => $"sensor {store} {inside} {type}";
    }
    #endregion
}
