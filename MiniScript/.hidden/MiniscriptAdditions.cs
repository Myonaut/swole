/*
// Some quick tests

v = vector.zero
q = quaternion.identity
m = matrix.identity4x4
print "1: " + v
print "2: " + q
print "3: " + m

vv = vec(1, 2, 3, 4)
qq = rot(0, 0.7071068, 0, 0.7071068)
mm = mat(vec(0, 1, 2, 3), vec(4, 5, 6, 7), vec(8, 9, 10, 11), vec(12, 13, 14, 15))
print "4: " + vv
print "5: " + qq
print "6: " + mm

vvv = v + vv
qqq = q * qq
mmm = m + mm
print "7: " + vvv
print "8: " + qqq
print "9: " + mmm

vvvv = vv * vvv
qqqq = qq * qqq
mmmm = mm * mmm
print "10: " + vvvv
print "11: " + qqqq
print "12: " + mmmm

print "13: " + vv.x
print "14: " + vv.y
print "15: " + vv.z
print "16: " + vv.w

print "17: " + vv.xy
print "18: " + vv.xyz
print "19: " + vv.xyzw

print "20: " + vv.yx
print "21: " + vv.zyx
print "22: " + vv.wzyx

v = 5 * vec(1, 0, 0, 0)
print "23: " + v.xyz + " x " + v.yxz + " = " + cross(v.xyz, v.yxz)
print "24: " + v.xyz + " dot  " + v.yxz + " = " + dot(v.xyz, v.yxz)
print "25: " + v.xyz + " dot  " + -v.xyz + " = " + dot(v.xyz, -v.xyz)

v = qq * v
print "26: " + v

m = mat(
vec(1, 0, 0, 0),
vec(0, 1, 0, 0),
vec(0, 0, 1, 0),
vec(1, 2, 3, 1))

v = transform(m, v)
print "27: " + v

*/

#region MiniscriptTypes.cs 
namespace MiniscriptTypes.cs 
{
    
    // ...

    #region ADDITIONS

    /// <summary>
    /// ValVector represents a MiniScript vector, which supports up to four components (x, y, z, w), each represented by a ValNumber.
    /// </summary>
    public class ValVector : Value
    {
        public const int maxComponentCount = 4;

        public ValNumber x, y, z, w;

        public static ValNumber Comp(ValNumber component) => component == null ? ValNumber.zero : component;

        public int ComponentCount
        {
            get
            {
                int count = 0;
                if (x != null)
                {
                    count++;
                    if (y != null)
                    {
                        count++;
                        if (z != null)
                        {
                            count++;
                            if (w != null) count++;
                        }
                    }
                }
                return count;
            }
        }

        public ValVector()
        {
            this.x = ValNumber.zero;
            this.y = ValNumber.zero;
        }
        public ValVector(ValNumber x, ValNumber y, ValNumber z = null, ValNumber w = null)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
        public ValVector(double x, double y) : this(new ValNumber(x), new ValNumber(y)) {}
        public ValVector(double x, double y, double z) : this(new ValNumber(x), new ValNumber(y), new ValNumber(z)) {}
        public ValVector(double x, double y, double z, double w) : this(new ValNumber(x), new ValNumber(y), new ValNumber(z), new ValNumber(w)) {}

        public ValVector EvalCopy(TAC.Context context)
        {
            var result = new ValVector();
            if (x != null)
            {
                result.x = (ValNumber)x.Val(context);
                if (y != null)
                {
                    result.y = (ValNumber)y.Val(context);
                    if (z != null)
                    {
                        result.z = (ValNumber)z.Val(context);
                        if (w != null)
                        {
                            result.w = (ValNumber)w.Val(context);
                        }
                    }
                }
            }
            return result;
        }

        public override string CodeForm(TAC.Machine vm, int recursionLimit = -1)
        {
            string args = string.Empty;
            if (x != null)
            {
                args = x.CodeForm(vm);
                if (y != null)
                {
                    args = args + ", " + y.CodeForm(vm);
                    if (z != null)
                    {
                        args = args + ", " + z.CodeForm(vm);
                        if (w != null)
                        {
                            args = args + ", " + w.CodeForm(vm);
                        }
                    }
                }
            }
            return $"vec({args})";
        }

        public override string ToString(TAC.Machine vm)
        {
            return CodeForm(vm);
        }

        public override bool BoolValue()
        {
            // A vector is considered true if one of its components are non-zero.
            if (x != null)
            {
                if (x.BoolValue()) return true;
                if (y != null)
                {
                    if (y.BoolValue()) return true;
                    if (z != null)
                    {
                        if (z.BoolValue()) return true;
                        if (w != null)
                        {
                            if (w.BoolValue()) return true;
                        }
                    }
                }
            }
            return false;
        }

        public override bool IsA(Value type, TAC.Machine vm)
        {
            return type == vm.vectorType;
        }

        public override int Hash()
        {
            return RecursiveHash();
        }

        public override double Equality(Value rhs) 
        {
            if (!(rhs is ValVector vec)) return 0;
            if (ComponentCount != vec.ComponentCount) return 0;
            if (x != null && x.Equality(vec.x) == 0.0) return 0;
            if (y != null && y.Equality(vec.y) == 0.0) return 0;
            if (z != null && z.Equality(vec.z) == 0.0) return 0;
            if (w != null && w.Equality(vec.w) == 0.0) return 0;
            return 1;
        }

        public override bool CanSetElem() { return true; }

        public override void SetElem(Value index, Value value)
        {
            if (!(value is ValNumber valueNumeric)) throw new TypeException("Vector component value must be numeric", null);
            var i = index.IntValue();
            if (i < 0) i += maxComponentCount;
            if (i < 0 || i >= maxComponentCount)
            {
                throw new IndexException("Index Error (vector component index " + index + " out of range)");
            }
            if (i == 0)
            {
                x = valueNumeric;
            } 
            else if (i == 1)
            {
                if (x == null) x = ValNumber.zero;
                y = valueNumeric;
            } 
            else if (i == 2)
            {
                if (x == null) x = ValNumber.zero;
                if (y == null) y = ValNumber.zero;
                z = valueNumeric;
            } 
            else
            {
                if (x == null) x = ValNumber.zero;
                if (y == null) y = ValNumber.zero;
                if (z == null) z = ValNumber.zero;
                w = valueNumeric;
            }
        }

        public Value GetElem(Value index)
        {
            if (!(index is ValNumber)) throw new KeyException("Vector component index must be numeric", null);
            var i = index.IntValue();
            if (i < 0) i += maxComponentCount;
            if (i < 0 || i >= maxComponentCount)
            {
                throw new IndexException("Index Error (vector component index " + index + " out of range)");

            }
            return i == 0 ? x : i == 1 ? y : i == 2 ? z : w;
        }

        static ValVector _empty = new ValVector();

        static ValVector _zero2 = new ValVector(ValNumber.zero, ValNumber.zero);
        static ValVector _zero3 = new ValVector(ValNumber.zero, ValNumber.zero, ValNumber.zero);
        static ValVector _zero4 = new ValVector(ValNumber.zero, ValNumber.zero, ValNumber.zero, ValNumber.zero);

        static ValVector _one2 = new ValVector(ValNumber.one, ValNumber.one);
        static ValVector _one3 = new ValVector(ValNumber.one, ValNumber.one, ValNumber.one);
        static ValVector _one4 = new ValVector(ValNumber.one, ValNumber.one, ValNumber.one, ValNumber.one);

        /// <summary>
        /// A vector with no components.
        /// IMPORTANT: do not alter the value of the object returned!
        /// </summary>
        public static ValVector empty { get { return _empty; } }

        /// <summary>
        /// IMPORTANT: do not alter the value of the object returned!
        /// </summary>
        public static ValVector zero2 { get { return _zero2; } }
        /// <summary>
        /// IMPORTANT: do not alter the value of the object returned!
        /// </summary>
        public static ValVector zero3 { get { return _zero3; } }
        /// <summary>
        /// IMPORTANT: do not alter the value of the object returned!
        /// </summary>
        public static ValVector zero4 { get { return _zero4; } }

        /// <summary>
        /// IMPORTANT: do not alter the value of the object returned!
        /// </summary>
        public static ValVector one2 { get { return _one2; } }
        /// <summary>
        /// IMPORTANT: do not alter the value of the object returned!
        /// </summary>
        public static ValVector one3 { get { return _one3; } }
        /// <summary>
        /// IMPORTANT: do not alter the value of the object returned!
        /// </summary>
        public static ValVector one4 { get { return _one4; } }

        public static bool Equality(ValVector vecA, ValVector vecB)
        {
            if (vecA == null) return vecB == null;
            if (vecB == null) return false;
            return vecA.Equality(vecB) != 0.0;
        }

        public static ValVector Plus(ValVector vec, double value) => vec == null ? empty : new ValVector(
            vec.x == null ? null : new ValNumber(vec.x.value + value), 
            vec.y == null ? null : new ValNumber(vec.y.value + value), 
            vec.z == null ? null : new ValNumber(vec.z.value + value), 
            vec.w == null ? null : new ValNumber(vec.w.value + value));
        public static ValVector Plus(double value, ValVector vec) => vec == null ? empty : new ValVector(
            vec.x == null ? null : new ValNumber(value + vec.x.value), 
            vec.y == null ? null : new ValNumber(value + vec.y.value), 
            vec.z == null ? null : new ValNumber(value + vec.z.value), 
            vec.w == null ? null : new ValNumber(value + vec.w.value));
        public static ValVector Plus(ValVector vecA, ValVector vecB) => vecA == null ? (vecB == null ? empty : vecB) : (vecB == null ? vecA : new ValVector(
            vecA.x == null ? vecB.x : new ValNumber(vecA.x.value + (vecB.x == null ? 0 : vecB.x.value)),
            vecA.y == null ? vecB.y : new ValNumber(vecA.y.value + (vecB.y == null ? 0 : vecB.y.value)),
            vecA.z == null ? vecB.z : new ValNumber(vecA.z.value + (vecB.z == null ? 0 : vecB.z.value)),
            vecA.w == null ? vecB.w : new ValNumber(vecA.w.value + (vecB.w == null ? 0 : vecB.w.value))));
        
        public static ValVector Minus(ValVector vec, double value) => vec == null ? empty : new ValVector(
            vec.x == null ? null : new ValNumber(vec.x.value - value),
            vec.y == null ? null : new ValNumber(vec.y.value - value), 
            vec.z == null ? null : new ValNumber(vec.z.value - value), 
            vec.w == null ? null : new ValNumber(vec.w.value - value));
        public static ValVector Minus(double value, ValVector vec) => vec == null ? empty : new ValVector(
            vec.x == null ? null : new ValNumber(value - vec.x.value),
            vec.y == null ? null : new ValNumber(value - vec.y.value), 
            vec.z == null ? null : new ValNumber(value - vec.z.value), 
            vec.w == null ? null : new ValNumber(value - vec.w.value));
        public static ValVector Minus(ValVector vecA, ValVector vecB) => vecA == null ? (vecB == null ? empty : vecB) : (vecB == null ? vecA : new ValVector(
            vecA.x == null ? (vecB.x == null ? null : new ValNumber(-vecB.x.value)) : new ValNumber(vecA.x.value - (vecB.x == null ? 0 : vecB.x.value)),
            vecA.y == null ? (vecB.y == null ? null : new ValNumber(-vecB.y.value)) : new ValNumber(vecA.y.value - (vecB.y == null ? 0 : vecB.y.value)),
            vecA.z == null ? (vecB.z == null ? null : new ValNumber(-vecB.z.value)) : new ValNumber(vecA.z.value - (vecB.z == null ? 0 : vecB.z.value)),
            vecA.w == null ? (vecB.w == null ? null : new ValNumber(-vecB.w.value)) : new ValNumber(vecA.w.value - (vecB.w == null ? 0 : vecB.w.value))));
        
        public static ValVector Times(ValVector vec, double value) => vec == null ? empty : new ValVector(
            vec.x == null ? null : new ValNumber(vec.x.value * value),
            vec.y == null ? null : new ValNumber(vec.y.value * value), 
            vec.z == null ? null : new ValNumber(vec.z.value * value), 
            vec.w == null ? null : new ValNumber(vec.w.value * value));
        public static ValVector Times(double value, ValVector vec) => vec == null ? empty : new ValVector(
            vec.x == null ? null : new ValNumber(value * vec.x.value),
            vec.y == null ? null : new ValNumber(value * vec.y.value), 
            vec.z == null ? null : new ValNumber(value * vec.z.value), 
            vec.w == null ? null : new ValNumber(value * vec.w.value));
        public static ValVector Times(ValVector vecA, ValVector vecB) => vecA == null ? (vecB == null ? empty : vecB) : (vecB == null ? vecA : new ValVector(
            vecA.x == null ? (vecB.x == null ? null : ValNumber.zero) : new ValNumber(vecA.x.value * (vecB.x == null ? 0 : vecB.x.value)),
            vecA.y == null ? (vecB.y == null ? null : ValNumber.zero) : new ValNumber(vecA.y.value * (vecB.y == null ? 0 : vecB.y.value)),
            vecA.z == null ? (vecB.z == null ? null : ValNumber.zero) : new ValNumber(vecA.z.value * (vecB.z == null ? 0 : vecB.z.value)),
            vecA.w == null ? (vecB.w == null ? null : ValNumber.zero) : new ValNumber(vecA.w.value * (vecB.w == null ? 0 : vecB.w.value))));
        
        public static ValVector DividedBy(ValVector vec, double value) => vec == null ? empty : new ValVector(
            vec.x == null ? null : new ValNumber(vec.x.value / value),
            vec.y == null ? null : new ValNumber(vec.y.value / value), 
            vec.z == null ? null : new ValNumber(vec.z.value / value), 
            vec.w == null ? null : new ValNumber(vec.w.value / value));
        public static ValVector DividedBy(double value, ValVector vec) => vec == null ? empty : new ValVector(
            vec.x == null ? null : new ValNumber(value / vec.x.value),
            vec.y == null ? null : new ValNumber(value / vec.y.value), 
            vec.z == null ? null : new ValNumber(value / vec.z.value), 
            vec.w == null ? null : new ValNumber(value / vec.w.value));
        public static ValVector DividedBy(ValVector vecA, ValVector vecB) => vecA == null ? (vecB == null ? empty : vecB) : (vecB == null ? vecA : new ValVector(
            vecA.x == null ? null : new ValNumber(vecA.x.value / (vecB.x == null ? 1 : vecB.x.value)),
            vecA.y == null ? null : new ValNumber(vecA.y.value / (vecB.y == null ? 1 : vecB.y.value)),
            vecA.z == null ? null : new ValNumber(vecA.z.value / (vecB.z == null ? 1 : vecB.z.value)),
            vecA.w == null ? null : new ValNumber(vecA.w.value / (vecB.w == null ? 1 : vecB.w.value))));
        
        public static ValVector Mod(ValVector vec, double value) => vec == null ? empty : new ValVector(
            vec.x == null ? null : new ValNumber(vec.x.value % value),
            vec.y == null ? null : new ValNumber(vec.y.value % value), 
            vec.z == null ? null : new ValNumber(vec.z.value % value), 
            vec.w == null ? null : new ValNumber(vec.w.value % value));
        public static ValVector Mod(double value, ValVector vec) => vec == null ? empty : new ValVector(
            vec.x == null ? null : new ValNumber(value % vec.x.value),
            vec.y == null ? null : new ValNumber(value % vec.y.value), 
            vec.z == null ? null : new ValNumber(value % vec.z.value), 
            vec.w == null ? null : new ValNumber(value % vec.w.value));
        public static ValVector Mod(ValVector vecA, ValVector vecB) => vecA == null ? (vecB == null ? empty : vecB) : (vecB == null ? vecA : new ValVector(
            vecA.x == null ? null : new ValNumber(vecA.x.value % (vecB.x == null ? 1 : vecB.x.value)),
            vecA.y == null ? null : new ValNumber(vecA.y.value % (vecB.y == null ? 1 : vecB.y.value)),
            vecA.z == null ? null : new ValNumber(vecA.z.value % (vecB.z == null ? 1 : vecB.z.value)),
            vecA.w == null ? null : new ValNumber(vecA.w.value % (vecB.w == null ? 1 : vecB.w.value))));
        
        public static ValVector Pow(ValVector vec, double value) => vec == null ? empty : new ValVector(
            vec.x == null ? null : new ValNumber(Math.Pow(vec.x.value, value)),
            vec.y == null ? null : new ValNumber(Math.Pow(vec.y.value, value)), 
            vec.z == null ? null : new ValNumber(Math.Pow(vec.z.value, value)), 
            vec.w == null ? null : new ValNumber(Math.Pow(vec.w.value, value)));
        public static ValVector Pow(double value, ValVector vec) => vec == null ? empty : new ValVector(
            vec.x == null ? null : new ValNumber(Math.Pow(value, vec.x.value)),
            vec.y == null ? null : new ValNumber(Math.Pow(value, vec.y.value)), 
            vec.z == null ? null : new ValNumber(Math.Pow(value, vec.z.value)), 
            vec.w == null ? null : new ValNumber(Math.Pow(value, vec.w.value)));
        public static ValVector Pow(ValVector vecA, ValVector vecB) => vecA == null ? (vecB == null ? empty : vecB) : (vecB == null ? vecA : new ValVector(
            vecA.x == null ? null : new ValNumber(vecB.x == null ? vecA.x.value : Math.Pow(vecA.x.value, vecB.x.value)),
            vecA.y == null ? null : new ValNumber(vecB.y == null ? vecA.y.value : Math.Pow(vecA.y.value, vecB.y.value)),
            vecA.z == null ? null : new ValNumber(vecB.z == null ? vecA.z.value : Math.Pow(vecA.z.value, vecB.z.value)),
            vecA.w == null ? null : new ValNumber(vecB.w == null ? vecA.w.value : Math.Pow(vecA.w.value, vecB.w.value))));

        public static void CrossProduct(double xA, double yA, double zA, double xB, double yB, double zB, out double xC, out double yC, out double zC)
        {
            //(vecA * vecB.yzx - vecA.yzx * vecB).yzx;
            double x1, y1, z1, x2, y2, z2;
            x1 = xA * yB;
            y1 = yA * zB;
            z1 = zA * xB;

            x2 = yA * xB;
            y2 = zA * yB;
            z2 = xA * zB;

            xC = y1 - y2;
            yC = z1 - z2;
            zC = x1 - x2;
        }
        public static ValVector CrossProduct(ValVector vecA, ValVector vecB)
        {
            if (vecA == null) return vecB == null ? ValVector.empty : vecB;
            if (vecB == null) return vecA;

            double xA = vecA.x == null ? 0 : vecA.x.value;
            double yA = vecA.y == null ? 0 : vecA.y.value;
            double zA = vecA.z == null ? 0 : vecA.z.value;

            double xB = vecB.x == null ? 0 : vecB.x.value;
            double yB = vecB.y == null ? 0 : vecB.y.value;
            double zB = vecB.z == null ? 0 : vecB.z.value;

            CrossProduct(xA, yA, zA, xB, yB, zB, out double xC, out double yC, out double zC);

            return new ValVector(xC, yC, zC);
        }

        public static double DotProduct(ValVector vecA, ValVector vecB)
        {
            double result = 0;
            if (vecA.x != null || vecB.x != null)
            {
                result += Comp(vecA.x).value * Comp(vecB.x).value;
                if (vecA.y != null || vecB.y != null)
                {
                    result += Comp(vecA.y).value * Comp(vecB.y).value;
                    if (vecA.z != null || vecB.z != null)
                    {
                        result += Comp(vecA.z).value * Comp(vecB.z).value;
                        if (vecA.w != null || vecB.w != null)
                        {
                            result += Comp(vecA.w).value * Comp(vecB.w).value;
                        }
                    }
                }
            }
            return result;
        }

        public static double LengthSquared(ValVector vec)
        {
            double result = 0;
            if (vec.x != null)
            {
                result += vec.x.value * vec.x.value;
                if (vec.y != null)
                {
                    result += vec.y.value * vec.y.value;
                    if (vec.z != null)
                    {
                        result += vec.z.value * vec.z.value;
                        if (vec.w != null)
                        {
                            result += vec.w.value * vec.w.value;
                        }
                    }
                }
            }
            return result;
        }

        public static double Length(ValVector vec) => Math.Sqrt(LengthSquared(vec));

    }

    /// <summary>
    /// ValQuaternion represents a quaternion (rotation) value.
    /// </summary>
    public class ValQuaternion : ValVector
    {

        /// <summary>
        /// The components of this quaternion in vector form.
        /// </summary>
        public ValVector value => new ValVector(x, y, z, w);

        public ValQuaternion()
        {
            this.x = ValNumber.zero;
            this.y = ValNumber.zero;
            this.z = ValNumber.zero;
            this.w = ValNumber.zero;
        }
        public ValQuaternion(ValNumber x, ValNumber y, ValNumber z, ValNumber w) : base(x, y, z, w) {}
        public ValQuaternion(double x, double y, double z, double w) : base(x, y, z, w) {}

        new public ValQuaternion EvalCopy(TAC.Context context)
        {
            var result = new ValQuaternion();
            if (x != null) result.x = (ValNumber)x.Val(context); else result.x = ValNumber.zero;
            if (y != null) result.y = (ValNumber)y.Val(context); else result.y = ValNumber.zero;
            if (z != null) result.z = (ValNumber)z.Val(context); else result.z = ValNumber.zero;
            if (w != null) result.w = (ValNumber)w.Val(context); else result.w = ValNumber.zero;
            return result;
        }

        public override string CodeForm(TAC.Machine vm, int recursionLimit = -1)
        {
            return $"rot({(x == null ? "0" : x.CodeForm(vm))}, {(y == null ? "0" : y.CodeForm(vm))}, {(z == null ? "0" : z.CodeForm(vm))}, {(w == null ? "0" : w.CodeForm(vm))})";
        }

        public override bool IsA(Value type, TAC.Machine vm)
        {
            return type == vm.quaternionType;
        }

        static ValQuaternion _identity = new ValQuaternion(ValNumber.zero, ValNumber.zero, ValNumber.zero, ValNumber.one);

        /// <summary>
        /// IMPORTANT: do not alter the value of the object returned!
        /// </summary>
        public static ValQuaternion identity { get { return _identity; } }

        public static bool Equality(ValQuaternion quatA, ValQuaternion quatB)
        {
            if (quatA == null) return quatB == null;
            if (quatB == null) return false;
            return quatA.Equality(quatB) != 0.0;
        }

        /// <summary>
        /// Returns the inverse of a quaternion value.
        /// </summary>
        public static ValQuaternion Inverse(ValQuaternion quat)
        {
            if (quat == null) return ValQuaternion.identity;
            //float4 x = q.value;
            //return (1.0f / dot(x, x)) * x * float4(-1.0f, -1.0f, -1.0f, 1.0f);

            double sqrX = quat.x.value * quat.x.value;
            double sqrY = quat.y.value * quat.y.value;
            double sqrZ = quat.z.value * quat.z.value;
            double sqrW = quat.w.value * quat.w.value;

            double dotRCP = 1.0 / (sqrX + sqrY + sqrZ + sqrW);

            return new ValQuaternion(dotRCP * quat.x.value * -1.0, dotRCP * quat.y.value * -1.0, dotRCP * quat.z.value * -1.0, dotRCP * quat.w.value);
        }

        /// <summary>
        /// Returns the result of transforming quatB by quatA.
        /// </summary>
        public static ValQuaternion Transform(ValQuaternion quatA, ValQuaternion quatB) 
        {
            if (quatA == null || quatB == null) return ValQuaternion.identity;
            //a.value.wwww * b.value + (a.value.xyzx * b.value.wwwx + a.value.yzxy * b.value.zxyy) * float4(1.0f, 1.0f, 1.0f, -1.0f) - a.value.zxyz * b.value.yzxz;
            double x1, y1, z1, w1, x2, y2, z2, w2, x3, y3, z3, w3;

            x1=y1=z1=w1 = quatA.w.value;
            x1 = x1 * quatB.x.value;
            y1 = y1 * quatB.y.value;
            z1 = z1 * quatB.z.value;
            w1 = w1 * quatB.w.value;

            x2 = (quatA.x.value * quatB.w.value) + (quatA.y.value * quatB.z.value);
            y2 = (quatA.y.value * quatB.w.value) + (quatA.z.value * quatB.x.value);
            z2 = (quatA.z.value * quatB.w.value) + (quatA.x.value * quatB.y.value);
            w2 = ((quatA.x.value * quatB.x.value) + (quatA.y.value * quatB.y.value)) * -1.0f;

            x3 = quatA.z.value * quatB.y.value;
            y3 = quatA.x.value * quatB.z.value;
            z3 = quatA.y.value * quatB.x.value;
            w3 = quatA.z.value * quatB.z.value;

            return new ValQuaternion(x1 + x2 - x3, y1 + y2 - y3, z1 + z2 - z3, w1 + w2 - w3);
        } 

        /// <summary>
        /// Returns the result of transforming vec by quat.
        /// </summary>
        public static ValVector Transform(ValQuaternion quat, ValVector vec)
        {
            if (quat == null || vec == null) return ValVector.zero3;
            //            float3 t = 2 * cross(q.value.xyz, v);
            //return v + q.value.w * t + cross(q.value.xyz, t);

            ValVector.CrossProduct(quat.x.value, quat.y.value, quat.z.value, vec.x.value, vec.y.value, vec.z.value, out double tX, out double tY, out double tZ);
            tX *= 2;
            tY *= 2;
            tZ *= 2;

            double tX2 = quat.w.value * tX;
            double tY2 = quat.w.value * tY;
            double tZ2 = quat.w.value * tZ;

            ValVector.CrossProduct(quat.x.value, quat.y.value, quat.z.value, tX, tY, tZ, out double tX3, out double tY3, out double tZ3);

            return new ValVector(vec.x.value + tX2 + tX3, vec.y.value + tY2 + tY3, vec.z.value + tZ2 + tZ3);
        }

    }

    /// <summary>
    /// ValMatrix represents a column major matrix, which supports up to four dimensions.
    /// </summary>
    public class ValMatrix : Value
    {
        public const int maxDimensions = 4;

        public ValVector c0, c1, c2, c3;

        public int ColumnCount
        {
            get
            {
                int count = 0;
                if (c0 != null)
                {
                    count++;
                    if (c1 != null)
                    {
                        count++;
                        if (c2 != null)
                        {
                            count++;
                            if (c3 != null) count++;
                        }
                    }
                }
                return count;
            }
        }

        public int RowCount
        {
            get
            {
                int count = 0;
                if (c0 != null)
                {
                    count = c0.ComponentCount;
                    if (c1 != null)
                    {
                        count = Math.Min(count, c1.ComponentCount);
                        if (c2 != null)
                        {
                            count = Math.Min(count, c2.ComponentCount);
                            if (c3 != null) count = Math.Min(count, c3.ComponentCount);
                        }
                    }
                }
                return count;
            }
        }

        public ValMatrix()
        {
            this.c0 = ValVector.zero2;
            this.c1 = ValVector.zero2;
        }
        public ValMatrix(ValVector c0, ValVector c1, ValVector c2 = null, ValVector c3 = null)
        {
            if (c0 != null && c0.ComponentCount <= 0) c0 = null;
            if (c1 != null && c1.ComponentCount <= 0) c1 = null;
            if (c2 != null && c2.ComponentCount <= 0) c2 = null;
            if (c3 != null && c3.ComponentCount <= 0) c3 = null;
            this.c0 = c0;
            this.c1 = c1;
            this.c2 = c2;
            this.c3 = c3;
        }

        public ValMatrix(float m00, float m01, float m02, float m03,
                float m10, float m11, float m12, float m13,
                float m20, float m21, float m22, float m23,
                float m30, float m31, float m32, float m33)
        {
            this.c0 = new ValVector(m00, m10, m20, m30);
            this.c1 = new ValVector(m01, m11, m21, m31);
            this.c2 = new ValVector(m02, m12, m22, m32);
            this.c3 = new ValVector(m03, m13, m23, m33);
        }

        public ValMatrix EvalCopy(TAC.Context context)
        {
            var result = new ValMatrix();
            if (c0 != null)
            {
                result.c0 = (ValVector)c0.Val(context);
                if (c1 != null)
                {
                    result.c1 = (ValVector)c1.Val(context);
                    if (c2 != null)
                    {
                        result.c2 = (ValVector)c2.Val(context);
                        if (c3 != null)
                        {
                            result.c3 = (ValVector)c3.Val(context);
                        }
                    }
                }
            }
            return result;
        }

        public override string CodeForm(TAC.Machine vm, int recursionLimit = -1)
        {
            string args = string.Empty;
            if (c0 != null)
            {
                args = c0.CodeForm(vm);
                if (c1 != null)
                {
                    args = args + ", " + c1.CodeForm(vm);
                    if (c2 != null)
                    {
                        args = args + ", " + c2.CodeForm(vm);
                        if (c3 != null)
                        {
                            args = args + ", " + c3.CodeForm(vm);
                        }
                    }
                }
            }
            return $"mat({args})";
        }

        public override string ToString(TAC.Machine vm)
        {
            return CodeForm(vm);
        }

        public override bool BoolValue()
        {
            // A matrix is considered true if one of its elements are non-zero.
            if (c0 != null)
            {
                if (c0.BoolValue()) return true;
                if (c1 != null)
                {
                    if (c1.BoolValue()) return true;
                    if (c2 != null)
                    {
                        if (c2.BoolValue()) return true;
                        if (c3 != null)
                        {
                            if (c3.BoolValue()) return true;
                        }
                    }
                }
            }
            return false;
        }

        public override bool IsA(Value type, TAC.Machine vm)
        {
            return type == vm.matrixType;
        }

        public override int Hash()
        {
            return RecursiveHash();
        }

        public override double Equality(Value rhs)
        {
            if (!(rhs is ValMatrix mat)) return 0;
            if (ColumnCount != mat.ColumnCount || RowCount != mat.RowCount) return 0;
            if (c0 != null && c0.Equality(mat.c0) == 0.0) return 0;
            if (c1 != null && c1.Equality(mat.c1) == 0.0) return 0;
            if (c2 != null && c2.Equality(mat.c2) == 0.0) return 0;
            if (c3 != null && c3.Equality(mat.c3) == 0.0) return 0;
            return 1;
        }

        public override bool CanSetElem() { return true; }

        public override void SetElem(Value index, Value value)
        {
            if (!(value is ValVector valueVector)) throw new TypeException("Matrix column value must be a vector", null);
            var i = index.IntValue();
            if (i < 0) i += maxDimensions;
            if (i < 0 || i >= maxDimensions)
            {
                throw new IndexException("Index Error (matrix column index " + index + " out of range)");
            }
            ValVector ColumnZero(int rows)
            {
                if (rows <= 2) return ValVector.zero2;
                if (rows == 3) return ValVector.zero3;
                return ValVector.zero4;
            }
            ValVector FillColumn()
            {
                int rows = Math.Min(valueVector.ComponentCount, RowCount);
                return ColumnZero(rows);
            }
            if (i == 0)
            {
                c0 = valueVector;
            }
            else if (i == 1)
            {
                if (c0 == null) c0 = FillColumn();
                c1 = valueVector;
            }
            else if (i == 2)
            {
                if (c0 == null) c0 = FillColumn();
                if (c1 == null) c1 = FillColumn();
                c2 = valueVector;
            }
            else
            {
                if (c0 == null) c0 = FillColumn();
                if (c1 == null) c1 = FillColumn();
                if (c2 == null) c2 = FillColumn();
                c3 = valueVector;
            }
        }

        public Value GetElem(Value index)
        {
            if (!(index is ValNumber)) throw new KeyException("Matrix column index must be numeric", null);
            var i = index.IntValue();
            if (i < 0) i += maxDimensions;
            if (i < 0 || i >= maxDimensions)
            {
                throw new IndexException("Index Error (matrix column index " + index + " out of range)");

            }
            return i == 0 ? c0 : i == 1 ? c1 : i == 2 ? c2 : c3;
        }

        static ValMatrix _identity2x2 = new ValMatrix(new ValVector(1.0, 0.0), new ValVector(0.0, 1.0));
        static ValMatrix _identity3x3 = new ValMatrix(new ValVector(1.0, 0.0, 0.0), new ValVector(0.0, 1.0, 0.0), new ValVector(0.0, 0.0, 1.0));
        static ValMatrix _identity4x4 = new ValMatrix(new ValVector(1.0, 0.0, 0.0, 0.0), new ValVector(0.0, 1.0, 0.0, 0.0), new ValVector(0.0, 0.0, 1.0, 0.0), new ValVector(0.0, 0.0, 0.0, 1.0));

        /// <summary>
        /// IMPORTANT: do not alter the value of the object returned!
        /// </summary>
        public static ValMatrix identity2x2 { get { return _identity2x2; } }
        /// <summary>
        /// IMPORTANT: do not alter the value of the object returned!
        /// </summary>
        public static ValMatrix identity3x3 { get { return _identity3x3; } }
        /// <summary>
        /// IMPORTANT: do not alter the value of the object returned!
        /// </summary>
        public static ValMatrix identity4x4 { get { return _identity4x4; } }

        public static bool Equality(ValMatrix matA, ValMatrix matB)
        {
            if (matA == null) return matB == null;
            if (matB == null) return false;
            return matA.Equality(matB) != 0.0;
        }

        /// <summary>
        /// Returns the result of a componentwise addition of a matrix and a scalar value.
        /// </summary>
        public static ValMatrix Plus(ValMatrix matrix, double val)
        {
            if (matrix == null) return new ValMatrix();
            return new ValMatrix(
                ValVector.Plus(matrix.c0 == null ? ValVector.empty : matrix.c0, val),
                ValVector.Plus(matrix.c1 == null ? ValVector.empty : matrix.c1, val),
                ValVector.Plus(matrix.c2 == null ? ValVector.empty : matrix.c2, val),
                ValVector.Plus(matrix.c3 == null ? ValVector.empty : matrix.c3, val));
        }
        /// <summary>
        /// Returns the result of a componentwise addition of a scalar value and a matrix.
        /// </summary>
        public static ValMatrix Plus(double val, ValMatrix matrix)
        {
            if (matrix == null) return new ValMatrix();
            return new ValMatrix(
                ValVector.Plus(val, matrix.c0 == null ? ValVector.empty : matrix.c0),
                ValVector.Plus(val, matrix.c1 == null ? ValVector.empty : matrix.c1),
                ValVector.Plus(val, matrix.c2 == null ? ValVector.empty : matrix.c2),
                ValVector.Plus(val, matrix.c3 == null ? ValVector.empty : matrix.c3));
        }
        /// <summary>
        /// Returns the result of a componentwise addition of two matrices.
        /// </summary>
        public static ValMatrix Plus(ValMatrix matA, ValMatrix matB)
        {
            if (matA == null) return matB;
            if (matB == null) return matA;
            return new ValMatrix(
                ValVector.Plus(matA.c0 == null ? ValVector.empty : matA.c0, matB.c0 == null ? ValVector.empty : matB.c0),
                ValVector.Plus(matA.c1 == null ? ValVector.empty : matA.c1, matB.c1 == null ? ValVector.empty : matB.c1),
                ValVector.Plus(matA.c2 == null ? ValVector.empty : matA.c2, matB.c2 == null ? ValVector.empty : matB.c2),
                ValVector.Plus(matA.c3 == null ? ValVector.empty : matA.c3, matB.c3 == null ? ValVector.empty : matB.c3));
        }

        /// <summary>
        /// Returns the result of a componentwise subtraction of a matrix and a scalar value.
        /// </summary>
        public static ValMatrix Minus(ValMatrix matrix, double val)
        {
            if (matrix == null) return new ValMatrix();
            return new ValMatrix(
                ValVector.Minus(matrix.c0 == null ? ValVector.empty : matrix.c0, val),
                ValVector.Minus(matrix.c1 == null ? ValVector.empty : matrix.c1, val),
                ValVector.Minus(matrix.c2 == null ? ValVector.empty : matrix.c2, val),
                ValVector.Minus(matrix.c3 == null ? ValVector.empty : matrix.c3, val));
        }
        /// <summary>
        /// Returns the result of a componentwise subtraction of a scalar value and a matrix.
        /// </summary>
        public static ValMatrix Minus(double val, ValMatrix matrix)
        {
            if (matrix == null) return new ValMatrix();
            return new ValMatrix(
                ValVector.Minus(val, matrix.c0 == null ? ValVector.empty : matrix.c0),
                ValVector.Minus(val, matrix.c1 == null ? ValVector.empty : matrix.c1),
                ValVector.Minus(val, matrix.c2 == null ? ValVector.empty : matrix.c2),
                ValVector.Minus(val, matrix.c3 == null ? ValVector.empty : matrix.c3));
        }
        /// <summary>
        /// Returns the result of a componentwise subtraction of two matrices.
        /// </summary>
        public static ValMatrix Minus(ValMatrix matA, ValMatrix matB)
        {
            if (matA == null) return matB;
            if (matB == null) return matA;
            return new ValMatrix(
                ValVector.Minus(matA.c0 == null ? ValVector.empty : matA.c0, matB.c0 == null ? ValVector.empty : matB.c0),
                ValVector.Minus(matA.c1 == null ? ValVector.empty : matA.c1, matB.c1 == null ? ValVector.empty : matB.c1),
                ValVector.Minus(matA.c2 == null ? ValVector.empty : matA.c2, matB.c2 == null ? ValVector.empty : matB.c2),
                ValVector.Minus(matA.c3 == null ? ValVector.empty : matA.c3, matB.c3 == null ? ValVector.empty : matB.c3));
        }

        /// <summary>
        /// Returns the result of a componentwise multiplication of a matrix and a scalar value.
        /// </summary>
        public static ValMatrix Times(ValMatrix matrix, double val)
        {
            if (matrix == null) return new ValMatrix();
            return new ValMatrix(
                ValVector.Times(matrix.c0 == null ? ValVector.empty : matrix.c0, val),
                ValVector.Times(matrix.c1 == null ? ValVector.empty : matrix.c1, val),
                ValVector.Times(matrix.c2 == null ? ValVector.empty : matrix.c2, val),
                ValVector.Times(matrix.c3 == null ? ValVector.empty : matrix.c3, val));
        }
        /// <summary>
        /// Returns the result of a componentwise multiplication of a scalar value and a matrix.
        /// </summary>
        public static ValMatrix Times(double val, ValMatrix matrix)
        {
            if (matrix == null) return new ValMatrix();
            return new ValMatrix(
                ValVector.Times(val, matrix.c0 == null ? ValVector.empty : matrix.c0),
                ValVector.Times(val, matrix.c1 == null ? ValVector.empty : matrix.c1),
                ValVector.Times(val, matrix.c2 == null ? ValVector.empty : matrix.c2),
                ValVector.Times(val, matrix.c3 == null ? ValVector.empty : matrix.c3));
        }
        /// <summary>
        /// Returns the result of a componentwise multiplication of two matrices.
        /// </summary>
        public static ValMatrix Times(ValMatrix matA, ValMatrix matB)
        {
            if (matA == null) return matB;
            if (matB == null) return matA;
            return new ValMatrix(
                ValVector.Times(matA.c0 == null ? ValVector.empty : matA.c0, matB.c0 == null ? ValVector.empty : matB.c0),
                ValVector.Times(matA.c1 == null ? ValVector.empty : matA.c1, matB.c1 == null ? ValVector.empty : matB.c1),
                ValVector.Times(matA.c2 == null ? ValVector.empty : matA.c2, matB.c2 == null ? ValVector.empty : matB.c2),
                ValVector.Times(matA.c3 == null ? ValVector.empty : matA.c3, matB.c3 == null ? ValVector.empty : matB.c3));
        }

        /// <summary>
        /// Returns the result of a componentwise division of a matrix and a scalar value.
        /// </summary>
        public static ValMatrix DividedBy(ValMatrix matrix, double val)
        {
            if (matrix == null) return new ValMatrix();
            return new ValMatrix(
                ValVector.DividedBy(matrix.c0 == null ? ValVector.empty : matrix.c0, val),
                ValVector.DividedBy(matrix.c1 == null ? ValVector.empty : matrix.c1, val),
                ValVector.DividedBy(matrix.c2 == null ? ValVector.empty : matrix.c2, val),
                ValVector.DividedBy(matrix.c3 == null ? ValVector.empty : matrix.c3, val));
        }
        /// <summary>
        /// Returns the result of a componentwise division of a scalar value and a matrix.
        /// </summary>
        public static ValMatrix DividedBy(double val, ValMatrix matrix)
        {
            if (matrix == null) return new ValMatrix();
            return new ValMatrix(
                ValVector.DividedBy(val, matrix.c0 == null ? ValVector.empty : matrix.c0),
                ValVector.DividedBy(val, matrix.c1 == null ? ValVector.empty : matrix.c1),
                ValVector.DividedBy(val, matrix.c2 == null ? ValVector.empty : matrix.c2),
                ValVector.DividedBy(val, matrix.c3 == null ? ValVector.empty : matrix.c3));
        }
        /// <summary>
        /// Returns the result of a componentwise division of two matrices.
        /// </summary>
        public static ValMatrix DividedBy(ValMatrix matA, ValMatrix matB)
        {
            if (matA == null) return matB;
            if (matB == null) return matA;
            return new ValMatrix(
                ValVector.DividedBy(matA.c0 == null ? ValVector.empty : matA.c0, matB.c0 == null ? ValVector.empty : matB.c0),
                ValVector.DividedBy(matA.c1 == null ? ValVector.empty : matA.c1, matB.c1 == null ? ValVector.empty : matB.c1),
                ValVector.DividedBy(matA.c2 == null ? ValVector.empty : matA.c2, matB.c2 == null ? ValVector.empty : matB.c2),
                ValVector.DividedBy(matA.c3 == null ? ValVector.empty : matA.c3, matB.c3 == null ? ValVector.empty : matB.c3));
        }

        /// <summary>
        /// Returns the result of a componentwise modulo of a matrix and a scalar value.
        /// </summary>
        public static ValMatrix Mod(ValMatrix matrix, double val)
        {
            if (matrix == null) return new ValMatrix();
            return new ValMatrix(
                ValVector.Mod(matrix.c0 == null ? ValVector.empty : matrix.c0, val),
                ValVector.Mod(matrix.c1 == null ? ValVector.empty : matrix.c1, val),
                ValVector.Mod(matrix.c2 == null ? ValVector.empty : matrix.c2, val),
                ValVector.Mod(matrix.c3 == null ? ValVector.empty : matrix.c3, val));
        }
        /// <summary>
        /// Returns the result of a componentwise modulo of a scalar value and a matrix.
        /// </summary>
        public static ValMatrix Mod(double val, ValMatrix matrix)
        {
            if (matrix == null) return new ValMatrix();
            return new ValMatrix(
                ValVector.Mod(val, matrix.c0 == null ? ValVector.empty : matrix.c0),
                ValVector.Mod(val, matrix.c1 == null ? ValVector.empty : matrix.c1),
                ValVector.Mod(val, matrix.c2 == null ? ValVector.empty : matrix.c2),
                ValVector.Mod(val, matrix.c3 == null ? ValVector.empty : matrix.c3));
        }
        /// <summary>
        /// Returns the result of a componentwise modulo of two matrices.
        /// </summary>
        public static ValMatrix Mod(ValMatrix matA, ValMatrix matB)
        {
            if (matA == null) return matB;
            if (matB == null) return matA;
            return new ValMatrix(
                ValVector.Mod(matA.c0 == null ? ValVector.empty : matA.c0, matB.c0 == null ? ValVector.empty : matB.c0),
                ValVector.Mod(matA.c1 == null ? ValVector.empty : matA.c1, matB.c1 == null ? ValVector.empty : matB.c1),
                ValVector.Mod(matA.c2 == null ? ValVector.empty : matA.c2, matB.c2 == null ? ValVector.empty : matB.c2),
                ValVector.Mod(matA.c3 == null ? ValVector.empty : matA.c3, matB.c3 == null ? ValVector.empty : matB.c3));
        }

        /// <summary>
        /// Returns the result of a componentwise exponentiation of a matrix and a scalar value.
        /// </summary>
        public static ValMatrix Pow(ValMatrix matrix, double val)
        {
            if (matrix == null) return new ValMatrix();
            return new ValMatrix(
                ValVector.Pow(matrix.c0 == null ? ValVector.empty : matrix.c0, val),
                ValVector.Pow(matrix.c1 == null ? ValVector.empty : matrix.c1, val),
                ValVector.Pow(matrix.c2 == null ? ValVector.empty : matrix.c2, val),
                ValVector.Pow(matrix.c3 == null ? ValVector.empty : matrix.c3, val));
        }
        /// <summary>
        /// Returns the result of a componentwise exponentiation of a scalar value and a matrix.
        /// </summary>
        public static ValMatrix Pow(double val, ValMatrix matrix)
        {
            if (matrix == null) return new ValMatrix();
            return new ValMatrix(
                ValVector.Pow(val, matrix.c0 == null ? ValVector.empty : matrix.c0),
                ValVector.Pow(val, matrix.c1 == null ? ValVector.empty : matrix.c1),
                ValVector.Pow(val, matrix.c2 == null ? ValVector.empty : matrix.c2),
                ValVector.Pow(val, matrix.c3 == null ? ValVector.empty : matrix.c3));
        }
        /// <summary>
        /// Returns the result of a componentwise exponentiation of two matrices.
        /// </summary>
        public static ValMatrix Pow(ValMatrix matA, ValMatrix matB)
        {
            if (matA == null) return matB;
            if (matB == null) return matA;
            return new ValMatrix(
                ValVector.Pow(matA.c0 == null ? ValVector.empty : matA.c0, matB.c0 == null ? ValVector.empty : matB.c0),
                ValVector.Pow(matA.c1 == null ? ValVector.empty : matA.c1, matB.c1 == null ? ValVector.empty : matB.c1),
                ValVector.Pow(matA.c2 == null ? ValVector.empty : matA.c2, matB.c2 == null ? ValVector.empty : matB.c2),
                ValVector.Pow(matA.c3 == null ? ValVector.empty : matA.c3, matB.c3 == null ? ValVector.empty : matB.c3));
        }

        /// <summary>
        /// Return the result of rotating a vector using a transformation matrix
        /// </summary>
        public static ValVector Rotate(ValMatrix matrix, ValVector vec)
        {
            if (matrix == null || vec == null) return ValVector.zero3;
            //(matrix.c0 * vec.x + matrix.c1 * vec.y + matrix.c2 * vec.z).xyz;
            double c00, c01, c02, c10, c11, c12, c20, c21, c22;
            c00 = c01 = c02 = c10 = c11 = c12 = c20 = c21 = c22 = 0;
            c00 = c11 = c22 = 1.0;
            if (matrix.c0 != null)
            {
                c00 = matrix.c0.x != null ? matrix.c0.x.value : c00;
                c01 = matrix.c0.y != null ? matrix.c0.y.value : c01;
                c02 = matrix.c0.z != null ? matrix.c0.z.value : c02;
            }
            if (matrix.c1 != null)
            {
                c10 = matrix.c1.x != null ? matrix.c1.x.value : c10;
                c11 = matrix.c1.y != null ? matrix.c1.y.value : c11;
                c12 = matrix.c1.z != null ? matrix.c1.z.value : c12;
            }
            if (matrix.c2 != null)
            {
                c20 = matrix.c2.x != null ? matrix.c2.x.value : c20;
                c21 = matrix.c2.y != null ? matrix.c2.y.value : c21;
                c22 = matrix.c2.z != null ? matrix.c2.z.value : c22;
            }

            double vX = ValVector.Comp(vec.x).value;
            double vY = ValVector.Comp(vec.y).value;
            double vZ = ValVector.Comp(vec.z).value;

            return new ValVector(
                (c00 * vX) + (c10 * vY) + (c20 * vZ),
                (c01 * vX) + (c11 * vY) + (c21 * vZ),
                (c02 * vX) + (c12 * vY) + (c22 * vZ));
        }

        /// <summary>
        /// Return the result of transforming a point using a transformation matrix
        /// </summary>
        public static ValVector Transform(ValMatrix matrix, ValVector vec)
        {
            if (matrix == null || vec == null) return ValVector.zero3;
            //(matrix.c0 * vec.x + matrix.c1 * vec.y + matrix.c2 * vec.z + matrix.c3).xyz;
            double c00, c01, c02, c10, c11, c12, c20, c21, c22, c30, c31, c32;
            c00 = c01 = c02 = c10 = c11 = c12 = c20 = c21 = c22 = c30 = c31 = c32 = 0;
            c00 = c11 = c22 = 1.0;
            if (matrix.c0 != null)
            {
                c00 = matrix.c0.x != null ? matrix.c0.x.value : c00;
                c01 = matrix.c0.y != null ? matrix.c0.y.value : c01;
                c02 = matrix.c0.z != null ? matrix.c0.z.value : c02;
            }
            if (matrix.c1 != null)
            {
                c10 = matrix.c1.x != null ? matrix.c1.x.value : c10;
                c11 = matrix.c1.y != null ? matrix.c1.y.value : c11;
                c12 = matrix.c1.z != null ? matrix.c1.z.value : c12;
            }
            if (matrix.c2 != null)
            {
                c20 = matrix.c2.x != null ? matrix.c2.x.value : c20;
                c21 = matrix.c2.y != null ? matrix.c2.y.value : c21;
                c22 = matrix.c2.z != null ? matrix.c2.z.value : c22;
            }
            if (matrix.c3 != null)
            {
                c30 = matrix.c3.x != null ? matrix.c3.x.value : c30;
                c31 = matrix.c3.y != null ? matrix.c3.y.value : c31;
                c32 = matrix.c3.z != null ? matrix.c3.z.value : c32;
            }

            double vX = ValVector.Comp(vec.x).value;
            double vY = ValVector.Comp(vec.y).value;
            double vZ = ValVector.Comp(vec.z).value;

            return new ValVector(
                (c00 * vX) + (c10 * vY) + (c20 * vZ) + c30,
                (c01 * vX) + (c11 * vY) + (c21 * vZ) + c31,
                (c02 * vX) + (c12 * vY) + (c22 * vZ) + c32);
        }

        /// <summary>
        /// Return the transpose of a matrix.
        /// </summary>
        public static ValMatrix Transpose(ValMatrix matrix)
        {
            if (matrix == null) return ValMatrix.identity4x4;
            /*double4x4(
                v.c0.x, v.c0.y, v.c0.z, v.c0.w,
                v.c1.x, v.c1.y, v.c1.z, v.c1.w,
                v.c2.x, v.c2.y, v.c2.z, v.c2.w,
                v.c3.x, v.c3.y, v.c3.z, v.c3.w);*/
            double c00, c01, c02, c03, c10, c11, c12, c13, c20, c21, c22, c23, c30, c31, c32, c33;
            c00 = c01 = c02 = c03 = c10 = c11 = c12 = c13 = c20 = c21 = c22 = c23 = c30 = c31 = c32 = c33 = 0;
            if (matrix.c0 != null)
            {
                c00 = matrix.c0.x != null ? matrix.c0.x.value : c00;
                c01 = matrix.c0.y != null ? matrix.c0.y.value : c01;
                c02 = matrix.c0.z != null ? matrix.c0.z.value : c02;
                c03 = matrix.c0.w != null ? matrix.c0.w.value : c03;
            }
            if (matrix.c1 != null)
            {
                c10 = matrix.c1.x != null ? matrix.c1.x.value : c10;
                c11 = matrix.c1.y != null ? matrix.c1.y.value : c11;
                c12 = matrix.c1.z != null ? matrix.c1.z.value : c12;
                c13 = matrix.c1.w != null ? matrix.c1.w.value : c13;
            }
            if (matrix.c2 != null)
            {
                c20 = matrix.c2.x != null ? matrix.c2.x.value : c20;
                c21 = matrix.c2.y != null ? matrix.c2.y.value : c21;
                c22 = matrix.c2.z != null ? matrix.c2.z.value : c22;
                c23 = matrix.c2.w != null ? matrix.c2.w.value : c23;
            }
            if (matrix.c3 != null)
            {
                c30 = matrix.c3.x != null ? matrix.c3.x.value : c30;
                c31 = matrix.c3.y != null ? matrix.c3.y.value : c31;
                c32 = matrix.c3.z != null ? matrix.c3.z.value : c32;
                c33 = matrix.c3.w != null ? matrix.c3.w.value : c33;
            }

            int rowCount = matrix.ColumnCount;
            int columnCount = matrix.RowCount;

            return new ValMatrix(
                columnCount > 0 ? new ValVector(rowCount > 0 ? new ValNumber(c00) : null, rowCount > 1 ? new ValNumber(c10) : null, rowCount > 2 ? new ValNumber(c20) : null, rowCount > 3 ? new ValNumber(c30) : null) : null,
                columnCount > 1 ? new ValVector(rowCount > 0 ? new ValNumber(c01) : null, rowCount > 1 ? new ValNumber(c11) : null, rowCount > 2 ? new ValNumber(c21) : null, rowCount > 3 ? new ValNumber(c31) : null) : null,
                columnCount > 2 ? new ValVector(rowCount > 0 ? new ValNumber(c02) : null, rowCount > 1 ? new ValNumber(c12) : null, rowCount > 2 ? new ValNumber(c22) : null, rowCount > 3 ? new ValNumber(c32) : null) : null,
                columnCount > 3 ? new ValVector(rowCount > 0 ? new ValNumber(c03) : null, rowCount > 1 ? new ValNumber(c13) : null, rowCount > 2 ? new ValNumber(c23) : null, rowCount > 3 ? new ValNumber(c33) : null) : null);
        }

    }

    #endregion

    public class ValSeqElem : Value {

        // ...

        public static Value Resolve(Value sequence, string identifier, TAC.Context context, out ValMap valueFoundIn) {
            // ...
            while (sequence != null) {
                // ...
                if (sequence is ValMap) {
                    // ...
                } else if (sequence is ValList) {
                    // ...
                } else if (sequence is ValString) {
                    // ...
                } else if (sequence is ValNumber) {
                    // ...
                } else if (sequence is ValFunction) {
                    // ...
                }
                #region ADDITIONS
                else if (sequence is ValVector)
                {
                    sequence = context.vm.vectorType ?? Intrinsics.VectorType();
                    includeMapType = false;
                }
                else if (sequence is ValQuaternion)
                {
                    sequence = context.vm.quaternionType ?? Intrinsics.QuaternionType();
                    includeMapType = false;
                }
                else if (sequence is ValMatrix)
                {
                    sequence = context.vm.matrixType ?? Intrinsics.MatrixType();
                    includeMapType = false;
                }
                #endregion
                else
                {
                    // ...
                }
                // ...
            }
        }

        // ...

    }

    // ...
    
}
#endregion

#region MiniscriptIntrinsics.cs
namespace MiniscriptIntrinsics.cs 
{
    
    public static class Intrinsics {
        
        // ...

        public static void InitIfNeeded() {

            // ...

            #region ADDITIONS

            // vector type
            //	Returns a vector that represents the vector datatype in
            //	MiniScript's core type system.  This can be used with `isa`
            //	to check whether a variable refers to a vector.  You can also
            //	assign new methods here to make them available to all vectors.
            f = Intrinsic.Create("vector");
            f.code = (context, partialResult) => {
                if (context.vm.vectorType == null)
                {
                    context.vm.vectorType = VectorType().EvalCopy(context.vm.globalContext);
                }
                return new Intrinsic.Result(context.vm.vectorType);
            };

            // quaternion type
            //	Returns a quaternion that represents the quaternion datatype in
            //	MiniScript's core type system.  This can be used with `isa`
            //	to check whether a variable refers to a quaternion.  You can also
            //	assign new methods here to make them available to all quaternions.
            f = Intrinsic.Create("quaternion");
            f.code = (context, partialResult) => {
                if (context.vm.quaternionType == null)
                {
                    context.vm.quaternionType = QuaternionType().EvalCopy(context.vm.globalContext);
                }
                return new Intrinsic.Result(context.vm.quaternionType);
            };

            // matrix type
            //	Returns a matrix that represents the matrix datatype in
            //	MiniScript's core type system.  This can be used with `isa`
            //	to check whether a variable refers to a matrix.  You can also
            //	assign new methods here to make them available to all matrices.
            f = Intrinsic.Create("matrix");
            f.code = (context, partialResult) => {
                if (context.vm.matrixType == null)
                {
                    context.vm.matrixType = MatrixType().EvalCopy(context.vm.globalContext);
                }
                return new Intrinsic.Result(context.vm.matrixType);
            };

            // constructor for vector type | vec(x,y,z,w)
            f = Intrinsic.Create("vec");
            f.AddParam("x", double.MinValue);
            f.AddParam("y", double.MinValue);
            f.AddParam("z", double.MinValue);
            f.AddParam("w", double.MinValue);
            f.code = (context, partialResult) => {
                double x = context.GetVar("x").DoubleValue();
                double y = context.GetVar("y").DoubleValue();
                double z = context.GetVar("z").DoubleValue();
                double w = context.GetVar("w").DoubleValue();
                return new Intrinsic.Result(new ValVector(
                    x == double.MinValue ? null : new ValNumber(x),
                    y == double.MinValue ? null : new ValNumber(y),
                    z == double.MinValue ? null : new ValNumber(z),
                    w == double.MinValue ? null : new ValNumber(w)));
            };

            // constructor for quaternion type | rot(x,y,z,w)
            f = Intrinsic.Create("rot");
            f.AddParam("x");
            f.AddParam("y");
            f.AddParam("z");
            f.AddParam("w");
            f.code = (context, partialResult) => {
                return new Intrinsic.Result(new ValQuaternion(
                    context.GetVar("x").DoubleValue(), 
                    context.GetVar("y").DoubleValue(), 
                    context.GetVar("z").DoubleValue(), 
                    context.GetVar("w").DoubleValue()));
            };

            // constructor for matrix type | mat(c0,c1,c2,c3)
            f = Intrinsic.Create("mat");
            f.AddParam("c0", ValVector.empty);
            f.AddParam("c1", ValVector.empty);
            f.AddParam("c2", ValVector.empty);
            f.AddParam("c3", ValVector.empty);
            f.code = (context, partialResult) => {
                return new Intrinsic.Result(new ValMatrix(
                    (ValVector)context.GetVar("c0"),
                    (ValVector)context.GetVar("c1"),
                    (ValVector)context.GetVar("c2"),
                    (ValVector)context.GetVar("c3")));
            };

            // self.sqrMagnitude
            //	Returns the square magnitude of a vector
            f = Intrinsic.Create("sqrMagnitude");
            f.AddParam("self");
            f.code = (context, partialResult) => {
                Value self = context.self;
                if (self is ValVector vec)
                {
                    return new Intrinsic.Result(new ValNumber(ValVector.LengthSquared(vec)));
                }
                return new Intrinsic.Result(self);
            };

            // self.magnitude
            //	Returns the magnitude of a vector
            f = Intrinsic.Create("magnitude");
            f.AddParam("self");
            f.code = (context, partialResult) => {
                Value self = context.self;
                if (self is ValVector vec)
                {
                    return new Intrinsic.Result(new ValNumber(ValVector.Length(vec)));
                }
                return new Intrinsic.Result(self);
            };

            // self.inverse
            //	Returns the inverse of a quaternion or a matrix
            f = Intrinsic.Create("inverse");
            f.AddParam("self");
            f.code = (context, partialResult) => {
                Value self = context.self;
                if (self is ValQuaternion quat)
                {
                    return new Intrinsic.Result(ValQuaternion.Inverse(quat));
                } 
                else if (self is ValMatrix mat)
                {
                    // TODO: Add support for inverse matrix
                    return new Intrinsic.Result(self);
                }
                return new Intrinsic.Result(self);
            };

            // dot
            //	Returns the dot product of two vectors
            f = Intrinsic.Create("dot");
            f.AddParam("vecA");
            f.AddParam("vecB");
            f.code = (context, partialResult) => {
                return new Intrinsic.Result(ValVector.DotProduct((ValVector)context.GetVar("vecA"), (ValVector)context.GetVar("vecB")));
            };

            // cross
            //	Returns the cross product of two vectors
            f = Intrinsic.Create("cross");
            f.AddParam("vecA");
            f.AddParam("vecB");
            f.code = (context, partialResult) => {
                return new Intrinsic.Result(ValVector.CrossProduct((ValVector)context.GetVar("vecA"), (ValVector)context.GetVar("vecB")));
            };

            // rotate
            //	Allows a quaternion or vector (rhs) to be transformed by a quaternion (lhs), or for a vector (rhs) to be rotated by a transformation matrix (lhs)
            f = Intrinsic.Create("rotate");
            f.AddParam("lhs");
            f.AddParam("rhs");
            f.code = (context, partialResult) => {
                Value lhs = context.GetVar("lhs");
                Value rhs = context.GetVar("rhs");
                if (lhs is ValQuaternion quatA)
                {
                    if (rhs is ValQuaternion quatB)
                    {
                        return new Intrinsic.Result(ValQuaternion.Transform(quatA, quatB));
                    }
                    else if (rhs is ValVector vecB)
                    {
                        return new Intrinsic.Result(ValQuaternion.Transform(quatA, vecB));
                    }
                    return new Intrinsic.Result(quatA);
                } 
                else if (lhs is ValMatrix matA)
                {
                    if (rhs is ValVector vecB)
                    {
                        return new Intrinsic.Result(ValMatrix.Rotate(matA, vecB));
                    }
                }
                return Intrinsic.Result.Null;
            };

            // transform
            //	Allows a point (rhs) to be transformed by a transformation matrix (lhs)
            f = Intrinsic.Create("transform");
            f.AddParam("lhs");
            f.AddParam("rhs");
            f.code = (context, partialResult) => {
                Value lhs = context.GetVar("lhs");
                Value rhs = context.GetVar("rhs");
                if (lhs is ValMatrix matA)
                {
                    if (rhs is ValVector vecB)
                    {
                        return new Intrinsic.Result(ValMatrix.Transform(matA, vecB));
                    }
                }
                return Intrinsic.Result.Null;
            };

            // vector swizzling
            for (int a = 0; a < 4; a++)
            {
                int a_ = a;
                string a__ = a == 0 ? "x" : a == 1 ? "y" : a == 2 ? "z" : "w";
                for (int b = 0; b < 4; b++)
                {
                    int b_ = b;
                    string b__ = b == 0 ? "x" : b == 1 ? "y" : b == 2 ? "z" : "w";

                    f = Intrinsic.Create(a__ + b__);
                    f.AddParam("self");
                    f.code = (context, partialResult) => {
                        Value self = context.self;
                        if (self is ValVector vec)
                        {
                            ValNumber x = a_ == 0 ? vec.x : a_ == 1 ? vec.y : a_ == 2 ? vec.z : vec.w;
                            ValNumber y = b_ == 0 ? vec.x : b_ == 1 ? vec.y : b_ == 2 ? vec.z : vec.w;
                            return new Intrinsic.Result(new ValVector(x, y));
                        }
                        return new Intrinsic.Result(self);
                    };

                    for (int c = 0; c < 4; c++)
                    {
                        int c_ = c;
                        string c__ = c == 0 ? "x" : c == 1 ? "y" : c == 2 ? "z" : "w";

                        f = Intrinsic.Create(a__ + b__ + c__);
                        f.AddParam("self");
                        f.code = (context, partialResult) => {
                            Value self = context.self;
                            if (self is ValVector vec)
                            {
                                ValNumber x = a_ == 0 ? vec.x : a_ == 1 ? vec.y : a_ == 2 ? vec.z : vec.w;
                                ValNumber y = b_ == 0 ? vec.x : b_ == 1 ? vec.y : b_ == 2 ? vec.z : vec.w;
                                ValNumber z = c_ == 0 ? vec.x : c_ == 1 ? vec.y : c_ == 2 ? vec.z : vec.w;
                                return new Intrinsic.Result(new ValVector(x, y, z));
                            }
                            return new Intrinsic.Result(self);
                        };

                        for (int d = 0; d < 4; d++)
                        {
                            int d_ = d;
                            string d__ = d == 0 ? "x" : d == 1 ? "y" : d == 2 ? "z" : "w";

                            f = Intrinsic.Create(a__+b__+c__+d__);
                            f.AddParam("self");
                            f.code = (context, partialResult) => {
                                Value self = context.self;
                                if (self is ValVector vec)
                                {
                                    ValNumber x = a_ == 0 ? vec.x : a_ == 1 ? vec.y : a_ == 2 ? vec.z : vec.w;
                                    ValNumber y = b_ == 0 ? vec.x : b_ == 1 ? vec.y : b_ == 2 ? vec.z : vec.w;
                                    ValNumber z = c_ == 0 ? vec.x : c_ == 1 ? vec.y : c_ == 2 ? vec.z : vec.w;
                                    ValNumber w = d_ == 0 ? vec.x : d_ == 1 ? vec.y : d_ == 2 ? vec.z : vec.w;
                                    return new Intrinsic.Result(new ValVector(x,y,z,w));
                                }
                                return new Intrinsic.Result(self);
                            };

                        }
                    }
                }
            }

            f = Intrinsic.Create("x");
            f.AddParam("self");
            f.code = (context, partialResult) => {
                Value self = context.self;
                if (self is ValVector vec)
                {
                    return new Intrinsic.Result(vec.x);
                }
                return new Intrinsic.Result(self);
            };

            f = Intrinsic.Create("y");
            f.AddParam("self");
            f.code = (context, partialResult) => {
                Value self = context.self;
                if (self is ValVector vec)
                {
                    return new Intrinsic.Result(vec.y);
                }
                return new Intrinsic.Result(self);
            };
             
            f = Intrinsic.Create("z");
            f.AddParam("self");
            f.code = (context, partialResult) => {
                Value self = context.self;
                if (self is ValVector vec)
                {
                    return new Intrinsic.Result(vec.z);
                }
                return new Intrinsic.Result(self);
            };

            f = Intrinsic.Create("w");
            f.AddParam("self");
            f.code = (context, partialResult) => {
                Value self = context.self;
                if (self is ValVector vec)
                {
                    return new Intrinsic.Result(vec.w);
                }
                return new Intrinsic.Result(self);
            };

            #endregion

        }

        #region ADDITIONS

        /// <summary>
        /// MatrixType: a static matrix that represents the Matrix type, and provides
        /// intrinsic methods that can be invoked on it via dot syntax.
        /// </summary>
        public static ValMap MatrixType()
        {
            if (_matrixType == null)
            {
                _matrixType = new ValMap();
                _matrixType["inverse"] = Intrinsic.GetByName("inverse").GetFunc();
                _matrixType["identity2x2"] = ValMatrix.identity2x2;
                _matrixType["identity3x3"] = ValMatrix.identity3x3;
                _matrixType["identity"] = _matrixType["identity4x4"] = ValMatrix.identity4x4;
            }
            return _matrixType;
        }
        static ValMap _matrixType = null;

        /// <summary>
        /// VectorType: a static vector that represents the Vector type, and provides
        /// intrinsic methods that can be invoked on it via dot syntax.
        /// </summary>
        public static ValMap VectorType()
        {
            if (_vectorType == null)
            {
                _vectorType = new ValMap();
                _vectorType["sqrMagnitude"] = Intrinsic.GetByName("sqrMagnitude").GetFunc();
                _vectorType["magnitude"] = Intrinsic.GetByName("magnitude").GetFunc();

                _vectorType["zero2"] = ValVector.zero2;
                _vectorType["zero"] = _vectorType["zero3"] = ValVector.zero3;
                _vectorType["zero4"] = ValVector.zero4;

                _vectorType["one2"] = ValVector.one2;
                _vectorType["one"] = _vectorType["one3"] = ValVector.one3;
                _vectorType["one4"] = ValVector.one4;

                // swizzling
                for (int a = 0; a < 4; a++)
                {
                    string a_ = a == 0 ? "x" : a == 1 ? "y" : a == 2 ? "z" : "w"; 
                    _vectorType[a_] = Intrinsic.GetByName(a_).GetFunc();
                    for (int b = 0; b < 4; b++)
                    {
                        string b_ = b == 0 ? "x" : b == 1 ? "y" : b == 2 ? "z" : "w";
                        _vectorType[a_ + b_] = Intrinsic.GetByName(a_ + b_).GetFunc();
                        for (int c = 0; c < 4; c++)
                        {
                            string c_ = c == 0 ? "x" : c == 1 ? "y" : c == 2 ? "z" : "w";
                            _vectorType[a_ + b_ + c_] = Intrinsic.GetByName(a_ + b_ + c_).GetFunc();
                            for (int d = 0; d < 4; d++)
                            {
                                string d_ = d == 0 ? "x" : d == 1 ? "y" : d == 2 ? "z" : "w";
                                _vectorType[a_+b_+c_+d_] = Intrinsic.GetByName(a_ + b_ + c_ + d_).GetFunc();
                            }
                        }
                    }
                }
            }
            return _vectorType;
        }
        static ValMap _vectorType = null;

        /// <summary>
        /// QuaternionType: a static quaternion that represents the Quaternion type, and provides
        /// intrinsic methods that can be invoked on it via dot syntax.
        /// </summary>
        public static ValMap QuaternionType()
        {
            if (_quaternionType == null)
            {
                _quaternionType = new ValMap();
                _quaternionType["inverse"] = Intrinsic.GetByName("inverse").GetFunc();
                _quaternionType["identity"] = ValQuaternion.identity;
            }
            return _quaternionType;
        }
        static ValMap _quaternionType = null;

        #endregion

    }
}
#endregion

#region MiniscriptTAC.cs
namespace MiniscriptTAC.cs {

    public static class TAC {

        public class Line {

            // ...

            public Value Evaluate(Context context) {

                if (opA is ValNumber) { 
                    
                    // ...

                    #region ADDITIONS
                    if (opB is ValVector vecB) {
                        switch (op)
                        {
                            case Op.APlusB:
                                return ValVector.Plus(fA, vecB);
                            case Op.AMinusB:
                                return ValVector.Minus(fA, vecB);
                            case Op.ATimesB:
                                return ValVector.Times(fA, vecB);
                            case Op.ADividedByB:
                                return ValVector.DividedBy(fA, vecB);
                            case Op.AModB:
                                return ValVector.Mod(fA, vecB);
                            case Op.APowB:
                                return ValVector.Pow(fA, vecB);
                            default:
                                break;
                        }
                    }
                    if (opB is ValMatrix matB) {
                        switch (op)
                        {
                            case Op.APlusB:
                                return ValMatrix.Plus(fA, matB);
                            case Op.AMinusB:
                                return ValMatrix.Minus(fA, matB);
                            case Op.ATimesB:
                                return ValMatrix.Times(fA, matB);
                            case Op.ADividedByB:
                                return ValMatrix.DividedBy(fA, matB);
                            case Op.AModB:
                                return ValMatrix.Mod(fA, matB);
                            case Op.APowB:
                                return ValMatrix.Pow(fA, matB);
                            default:
                                break;
                        }
                    }
                    #endregion

                    // ...

                } else if (opA is ValString) { 
                    // ...
                } else if (opA is ValList) { 
                    // ...
                } else if (opA is ValMap) { 
                    // ...
                } else if (opA is ValFunction && opB is ValFunction) { 
                    // ...
                }
                #region ADDITIONS
                else if (opA is ValQuaternion quatA)
                {
                    if (opB is ValQuaternion quatB)
                    {
                        switch (op)
                        {
                            case Op.ATimesB:
                                return ValQuaternion.Transform(quatA, quatB);
                            case Op.AEqualB:
                                return ValNumber.Truth(ValQuaternion.Equality(quatA, quatB));
                            case Op.ANotEqualB:
                                return ValNumber.Truth(!ValQuaternion.Equality(quatA, quatB));
                            default:
                                break;
                        }
                    }
                    else if (opB is ValVector vecB)
                    {
                        switch (op)
                        {
                            case Op.ATimesB:
                                return ValQuaternion.Transform(quatA, vecB);
                            default:
                                break;
                        }
                    }
                    // Handle equality testing between a quaternion (opA) and a non-quaternion (opB).
                    // These are always considered unequal.
                    if (op == Op.AEqualB) return ValNumber.zero;
                    if (op == Op.ANotEqualB) return ValNumber.one;

                }
                else if (opA is ValVector vecA)
                {
                    switch (op)
                    {
                        case Op.NotA:
                            return new ValVector(
                            vecA.x == null ? null : new ValNumber(1.0 - AbsClamp01(vecA.x.value)),
                            vecA.y == null ? null : new ValNumber(1.0 - AbsClamp01(vecA.y.value)),
                            vecA.z == null ? null : new ValNumber(1.0 - AbsClamp01(vecA.z.value)),
                            vecA.w == null ? null : new ValNumber(1.0 - AbsClamp01(vecA.w.value))
                            );
                    }
                    if (opB is ValVector vecB)
                    {
                        switch (op)
                        {
                            case Op.APlusB:
                                return ValVector.Plus(vecA, vecB);
                            case Op.AMinusB:
                                return ValVector.Minus(vecA, vecB);
                            case Op.ATimesB:
                                return ValVector.Times(vecA, vecB);
                            case Op.ADividedByB:
                                return ValVector.DividedBy(vecA, vecB);
                            case Op.AModB:
                                return ValVector.Mod(vecA, vecB);
                            case Op.APowB:
                                return ValVector.Pow(vecA, vecB);
                            case Op.AEqualB:
                                return ValNumber.Truth(ValVector.Equality(vecA, vecB));
                            case Op.ANotEqualB:
                                return ValNumber.Truth(!ValVector.Equality(vecA, vecB));
                            default:
                                break;
                        }
                    }
                    else if (opB is ValNumber || opB == null)
                    {
                        double fB = opB != null ? ((ValNumber)opB).value : 0;
                        switch (op)
                        {
                            case Op.APlusB:
                                return ValVector.Plus(vecA, fB);
                            case Op.AMinusB:
                                return ValVector.Minus(vecA, fB);
                            case Op.ATimesB:
                                return ValVector.Times(vecA, fB);
                            case Op.ADividedByB:
                                return ValVector.DividedBy(vecA, fB);
                            case Op.AModB:
                                return ValVector.Mod(vecA, fB);
                            case Op.APowB:
                                return ValVector.Pow(vecA, fB);
                            default:
                                break;
                        }
                    }
                    // Handle equality testing between a vector (opA) and a non-vector (opB).
                    // These are always considered unequal.
                    if (op == Op.AEqualB) return ValNumber.zero;
                    if (op == Op.ANotEqualB) return ValNumber.one;

                }
                else if (opA is ValMatrix matA)
                {
                    if (opB is ValMatrix matB)
                    {
                        switch (op)
                        {
                            case Op.APlusB:
                                return ValMatrix.Plus(matA, matB);
                            case Op.AMinusB:
                                return ValMatrix.Minus(matA, matB);
                            case Op.ATimesB:
                                return ValMatrix.Times(matA, matB);
                            case Op.ADividedByB:
                                return ValMatrix.DividedBy(matA, matB);
                            case Op.AModB:
                                return ValMatrix.Mod(matA, matB);
                            case Op.APowB:
                                return ValMatrix.Pow(matA, matB);
                            case Op.AEqualB:
                                return ValNumber.Truth(ValMatrix.Equality(matA, matB));
                            case Op.ANotEqualB:
                                return ValNumber.Truth(!ValMatrix.Equality(matA, matB));
                            default:
                                break;
                        }
                    }
                    else if (opB is ValNumber || opB == null)
                    {
                        double fB = opB != null ? ((ValNumber)opB).value : 0;
                        switch (op)
                        {
                            case Op.APlusB:
                                return ValMatrix.Plus(matA, fB);
                            case Op.AMinusB:
                                return ValMatrix.Minus(matA, fB);
                            case Op.ATimesB:
                                return ValMatrix.Times(matA, fB);
                            case Op.ADividedByB:
                                return ValMatrix.DividedBy(matA, fB);
                            case Op.AModB:
                                return ValMatrix.Mod(matA, fB);
                            case Op.APowB:
                                return ValMatrix.Pow(matA, fB);
                            default:
                                break;
                        }
                    }
                    // Handle equality testing between a matrix (opA) and a non-matrix (opB).
                    // These are always considered unequal.
                    if (op == Op.AEqualB) return ValNumber.zero;
                    if (op == Op.ANotEqualB) return ValNumber.one;

                }
                #endregion
                else
                {
                    // ...
                }
                
                // ...

            }

        }
        
        public class Machine { 
            
            // ...

            #region ADDITIONS
            public ValMap vectorType;
            public ValMap quaternionType;
            public ValMap matrixType;
            #endregion
            
            // ...

        }

        // ...
        
    }
}


#endregion