namespace MindustryCompiler.Code
{
    public static class Operators
    {
        public static Operation.Op GetOperator(string op)
        {
            switch (op)
            {
                case "+": return Operation.Op.add;
                case "-": return Operation.Op.sub;
                case "*": return Operation.Op.mul;
                case "/": return Operation.Op.div;
                case "//": return Operation.Op.idiv;
                case "%": return Operation.Op.mod;
                case "pow": return Operation.Op.pow;
                case "==": return Operation.Op.equal;
                case "!=": return Operation.Op.notEqual;
                case "&&": return Operation.Op.land;
                case "<": return Operation.Op.lessThan;
                case "<=": return Operation.Op.lessThanEq;
                case ">": return Operation.Op.greaterThan;
                case ">=": return Operation.Op.greaterThanEq;
                case "===": return Operation.Op.strictEqual;
                case "<<": return Operation.Op.shl;
                case ">>": return Operation.Op.shr;
                case "|": return Operation.Op.or;
                case "&": return Operation.Op.and;
                case "^": return Operation.Op.xor;
                case "!": return Operation.Op.not;
                case "max": return Operation.Op.max;
                case "min": return Operation.Op.min;
                case "angle": return Operation.Op.angle;
                case "len": return Operation.Op.len;
                case "noise": return Operation.Op.noise;
                case "abs": return Operation.Op.abs;
                case "log": return Operation.Op.log;
                case "log10": return Operation.Op.log10;
                case "sin": return Operation.Op.sin;
                case "cos": return Operation.Op.cos;
                case "tan": return Operation.Op.tan;
                case "floor": return Operation.Op.floor;
                case "ceil": return Operation.Op.ceil;
                case "sqrt": return Operation.Op.sqrt;
                case "rand": return Operation.Op.rand;

                default:
                    throw new InvalidOperatorException();
            }
        }

        public static Jump.JumpCondition GetCondition(string cond)
        {
            switch (cond)
            {
                case "==": return Jump.JumpCondition.equal;
                case "!=": return Jump.JumpCondition.notEqual;
                case "<": return Jump.JumpCondition.lessThan;
                case "<=": return Jump.JumpCondition.lessThanEq;
                case ">": return Jump.JumpCondition.greaterThan;
                case ">=": return Jump.JumpCondition.greaterThanEq;
                case "===": return Jump.JumpCondition.strictEqual;

                default:
                    throw new InvalidOperatorException();
            }
        }

        
    }
}
