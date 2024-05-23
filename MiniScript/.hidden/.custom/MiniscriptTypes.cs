/*	MiniscriptTypes.cs

Classes in this file represent the MiniScript type system.  Value is the 
abstract base class for all of them (i.e., represents ANY value in MiniScript),
from which more specific types are derived.

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Drawing.Drawing2D;

namespace Miniscript {
	
	/// <summary>
	/// Value: abstract base class for the MiniScript type hierarchy.
	/// Defines a number of handy methods that you can call on ANY
	/// value (though some of these do nothing for some types).
	/// </summary>
	public abstract class Value {
		/// <summary>
		/// Get the current value of this Value in the given context.  Basic types
		/// evaluate to themselves, but some types (e.g. variable references) may
		/// evaluate to something else.
		/// </summary>
		/// <param name="context">TAC context to evaluate in</param>
		/// <returns>value of this value (possibly the same as this)</returns>
		public virtual Value Val(TAC.Context context) {
			return this;		// most types evaluate to themselves
		}
		
		public override string ToString() {
			return ToString(null);
		}
		
		public abstract string ToString(TAC.Machine vm);
		
		/// <summary>
		/// This version of Val is like the one above, but also returns
		/// (via the output parameter) the ValMap the value was found in,
		/// which could be several steps up the __isa chain.
		/// </summary>
		/// <returns>The value.</returns>
		/// <param name="context">Context.</param>
		/// <param name="valueFoundIn">Value found in.</param>
		public virtual Value Val(TAC.Context context, out ValMap valueFoundIn) {
			valueFoundIn = null;
			return this;
		}
		
		/// <summary>
		/// Similar to Val, but recurses into the sub-values contained by this
		/// value (if it happens to be a container, such as a list or map).
		/// </summary>
		/// <param name="context">context in which to evaluate</param>
		/// <returns>fully-evaluated value</returns>
		public virtual Value FullEval(TAC.Context context) {
			return this;
		}
		
		/// <summary>
		/// Get the numeric value of this Value as an integer.
		/// </summary>
		/// <returns>this value, as signed integer</returns>
		public virtual int IntValue() {
			return (int)DoubleValue();
		}
		
		/// <summary>
		/// Get the numeric value of this Value as an unsigned integer.
		/// </summary>
		/// <returns>this value, as unsigned int</returns>
		public virtual uint UIntValue() {
			return (uint)DoubleValue();
		}
		
		/// <summary>
		/// Get the numeric value of this Value as a single-precision float.
		/// </summary>
		/// <returns>this value, as a float</returns>
		public virtual float FloatValue() {
			return (float)DoubleValue();
		}
		
		/// <summary>
		/// Get the numeric value of this Value as a double-precision floating-point number.
		/// </summary>
		/// <returns>this value, as a double</returns>
		public virtual double DoubleValue() {
			return 0;				// most types don't have a numeric value
		}
		
		/// <summary>
		/// Get the boolean (truth) value of this Value.  By default, we consider
		/// any numeric value other than zero to be true.  (But subclasses override
		/// this with different criteria for strings, lists, and maps.)
		/// </summary>
		/// <returns>this value, as a bool</returns>
		public virtual bool BoolValue() {
			return IntValue() != 0;
		}
		
		/// <summary>
		/// Get this value in the form of a MiniScript literal.
		/// </summary>
		/// <param name="recursionLimit">how deeply we can recurse, or -1 for no limit</param>
		/// <returns></returns>
		public virtual string CodeForm(TAC.Machine vm, int recursionLimit=-1) {
			return ToString(vm);
		}
		
		/// <summary>
		/// Get a hash value for this Value.  Two values that are considered
		/// equal will return the same hash value.
		/// </summary>
		/// <returns>hash value</returns>
		public abstract int Hash();
		
		/// <summary>
		/// Check whether this Value is equal to another Value.
		/// </summary>
		/// <param name="rhs">other value to compare to</param>
		/// <returns>1 if these values are considered equal; 0 if not equal; 0.5 if unsure</returns>
		public abstract double Equality(Value rhs);
		
		/// <summary>
		/// Can we set elements within this value?  (I.e., is it a list or map?)
		/// </summary>
		/// <returns>true if SetElem can work; false if it does nothing</returns>
		public virtual bool CanSetElem() { return false; }
		
		/// <summary>
		/// Set an element associated with the given index within this Value.
		/// </summary>
		/// <param name="index">index/key for the value to set</param>
		/// <param name="value">value to set</param>
		public virtual void SetElem(Value index, Value value) {}

		/// <summary>
		/// Return whether this value is the given type (or some subclass thereof)
		/// in the context of the given virtual machine.
		/// </summary>
		public virtual bool IsA(Value type, TAC.Machine vm) {
			return false;
		}

		/// <summary>
		/// Compare two Values for sorting purposes.
		/// </summary>
		public static int Compare(Value x, Value y) {
			// Always sort null to the end of the list.
			if (x == null) {
				if (y == null) return 0;
				return 1;
            }
			if (y == null) return -1;
			// If either argument is a string, do a string comparison
			if (x is ValString || y is ValString) {
				var sx = x.ToString();
				var sy = y.ToString();
				return sx.CompareTo(sy);
			}
			// If both arguments are numbers, compare numerically
			if (x is ValNumber && y is ValNumber) {
				double fx = ((ValNumber)x).value;
				double fy = ((ValNumber)y).value;
				if (fx < fy) return -1;
				if (fx > fy) return 1;
				return 0;
			}
			// Otherwise, consider all values equal, for sorting purposes.
			return 0;
		}

		private int RotateBits(int n) {
			return (n >> 1) | (n << (sizeof(int) * 8 - 1));
		}

		/// <summary>
		/// Compare lhs and rhs for equality, in a way that traverses down
		/// the tree when it finds a list or map.  For any other type, this
		/// just calls through to the regular Equality method.
		///
		/// Note that this works correctly for loops (maintaining a visited
		/// list to avoid recursing indefinitely).
		/// </summary>
		protected bool RecursiveEqual(Value rhs) { 
			var toDo = new Stack<ValuePair>();
			var visited = new HashSet<ValuePair>();
			toDo.Push(new ValuePair() { a = this, b = rhs });
			while (toDo.Count > 0) {
				var pair = toDo.Pop();
				visited.Add(pair);
				if (pair.a is ValList listA) {
					var listB = pair.b as ValList;
					if (listB == null) return false;
					int aCount = listA.values.Count;
					if (aCount != listB.values.Count) return false;
					for (int i = 0; i < aCount; i++) {
						var newPair = new ValuePair() {  a = listA.values[i], b = listB.values[i] };
						if (!visited.Contains(newPair)) toDo.Push(newPair);
					}
				} else if (pair.a is ValMap mapA) {
					var mapB = pair.b as ValMap;
					if (mapB == null) return false;
					if (mapA.map.Count != mapB.map.Count) return false;
					foreach (KeyValuePair<Value, Value> kv in mapA.map) {
						Value valFromB;
						if (!mapB.map.TryGetValue(kv.Key, out valFromB)) return false;
						Value valFromA = mapA.map[kv.Key];
						var newPair = new ValuePair() {  a = valFromA, b = valFromB };
						if (!visited.Contains(newPair)) toDo.Push(newPair);
					}
				} else if (pair.a == null || pair.b == null) {
					if (pair.a != null || pair.b != null) return false;
				} else {
					// No other types can recurse, so we can safely do:
					if (pair.a.Equality(pair.b) == 0) return false;
				}
			}
			// If we clear out our toDo list without finding anything unequal,
			// then the values as a whole must be equal.
			return true;
		}

		// Hash function that works correctly with nested lists and maps.
		protected int RecursiveHash()
		{
			int result = 0;
			var toDo = new Stack<Value>();
			var visited = new HashSet<Value>();
			toDo.Push(this);
			while (toDo.Count > 0) {
				Value item = toDo.Pop();
				visited.Add(item);
				if (item is ValList list) {
					result = RotateBits(result) ^ list.values.Count.GetHashCode();
					for (int i=list.values.Count-1; i>=0; i--) {
						Value child = list.values[i];
						if (!(child is ValList || child is ValMap) || !visited.Contains(child)) {
							toDo.Push(child);
						}
					}
				} else  if (item is ValMap map) {
					result = RotateBits(result) ^ map.map.Count.GetHashCode();
					foreach (KeyValuePair<Value, Value> kv in map.map) {
						if (!(kv.Key is ValList || kv.Key is ValMap) || !visited.Contains(kv.Key)) {
							toDo.Push(kv.Key);
						}
						if (!(kv.Value is ValList || kv.Value is ValMap) || !visited.Contains(kv.Value)) {
							toDo.Push(kv.Value);
						}
					}
				} else {
					// Anything else, we can safely use the standard hash method
					result = RotateBits(result) ^ (item == null ? 0 : item.Hash());
				}
			}
			return result;
		}
	}

	// ValuePair: used internally when working out whether two maps
	// or lists are equal.
	struct ValuePair {
		public Value a;
		public Value b;
	}

	public class ValueSorter : IComparer<Value>
	{
		public static ValueSorter instance = new ValueSorter();
		public int Compare(Value x, Value y)
		{
			return Value.Compare(x, y);
		}
	}

	public class ValueReverseSorter : IComparer<Value>
	{
		public static ValueReverseSorter instance = new ValueReverseSorter();
		public int Compare(Value x, Value y)
		{
			return Value.Compare(y, x);
		}
	}

	/// <summary>
	/// ValNull is an object to represent null in places where we can't use
	/// an actual null (such as a dictionary key or value).
	/// </summary>
	public class ValNull : Value {
		private ValNull() {}
		
		public override string ToString(TAC.Machine machine) {
			return "null";
		}
		
		public override bool IsA(Value type, TAC.Machine vm) {
			return false;
		}

		public override int Hash() {
			return -1;
		}

		public override Value Val(TAC.Context context) {
			return null;
		}

		public override Value Val(TAC.Context context, out ValMap valueFoundIn) {
			valueFoundIn = null;
			return null;
		}
		
		public override Value FullEval(TAC.Context context) {
			return null;
		}
		
		public override int IntValue() {
			return 0;
		}

		public override double DoubleValue() {
			return 0.0;
		}
		
		public override bool BoolValue() {
			return false;
		}

		public override double Equality(Value rhs) {
			return (rhs == null || rhs is ValNull ? 1 : 0);
		}

		static readonly ValNull _inst = new ValNull();
		
		/// <summary>
		/// Handy accessor to a shared "instance".
		/// </summary>
		public static ValNull instance { get { return _inst; } }
		
	}
	
	/// <summary>
	/// ValNumber represents a numeric (double-precision floating point) value in MiniScript.
	/// Since we also use numbers to represent boolean values, ValNumber does that job too.
	/// </summary>
	public class ValNumber : Value {
		public double value;

		public ValNumber(double value) {
			this.value = value;
		}

		public override string ToString(TAC.Machine vm) {
			// Convert to a string in the standard MiniScript way.
			if (value % 1.0 == 0.0) {
				// integer values as integers
				return value.ToString("0", CultureInfo.InvariantCulture);
			} else if (value > 1E10 || value < -1E10 || (value < 1E-6 && value > -1E-6)) {
				// very large/small numbers in exponential form
				string s = value.ToString("E6", CultureInfo.InvariantCulture);
				s = s.Replace("E-00", "E-0");
				return s;
			} else {
				// all others in decimal form, with 1-6 digits past the decimal point
				return value.ToString("0.0#####", CultureInfo.InvariantCulture);
			}
		}

		public override int IntValue() {
			return (int)value;
		}

		public override double DoubleValue() {
			return value;
		}
		
		public override bool BoolValue() {
			// Any nonzero value is considered true, when treated as a bool.
			return value != 0;
		}

		public override bool IsA(Value type, TAC.Machine vm) {
			return type == vm.numberType;
		}

		public override int Hash() {
			return value.GetHashCode();
		}

		public override double Equality(Value rhs) {
			return rhs is ValNumber && ((ValNumber)rhs).value == value ? 1 : 0;
		}

		static ValNumber _zero = new ValNumber(0), _one = new ValNumber(1);
		
		/// <summary>
		/// Handy accessor to a shared "zero" (0) value.
		/// IMPORTANT: do not alter the value of the object returned!
		/// </summary>
		public static ValNumber zero { get { return _zero; } }
		
		/// <summary>
		/// Handy accessor to a shared "one" (1) value.
		/// IMPORTANT: do not alter the value of the object returned!
		/// </summary>
		public static ValNumber one { get { return _one; } }
		
		/// <summary>
		/// Convenience method to get a reference to zero or one, according
		/// to the given boolean.  (Note that this only covers Boolean
		/// truth values; MiniScript also allows fuzzy truth values, like
		/// 0.483, but obviously this method won't help with that.)
		/// IMPORTANT: do not alter the value of the object returned!
		/// </summary>
		/// <param name="truthValue">whether to return 1 (true) or 0 (false)</param>
		/// <returns>ValNumber.one or ValNumber.zero</returns>
		public static ValNumber Truth(bool truthValue) {
			return truthValue ? one : zero;
		}
		
		/// <summary>
		/// Basically this just makes a ValNumber out of a double,
		/// BUT it is optimized for the case where the given value
		///	is either 0 or 1 (as is usually the case with truth tests).
		/// </summary>
		public static ValNumber Truth(double truthValue) {
			if (truthValue == 0.0) return zero;
			if (truthValue == 1.0) return one;
			return new ValNumber(truthValue);
		}
	}

    #region ADDITIONS

    /// <summary>
    /// ValVector represents a MiniScript vector, which supports up to four components (x, y, z, w), each represented by a ValNumber.
    /// </summary>
    public class ValVector : Value
    {
		public const int maxComponentCount = 4;

		public ValNumber x, y, z, w;

		#region Swizzling

		public static ValNumber Comp(ValNumber component) => component == null ? ValNumber.zero : component;

        #region xx

        public ValVector xx => new ValVector(Comp(x), Comp(x));
        public ValVector xy => new ValVector(Comp(x), Comp(y));
        public ValVector xz => new ValVector(Comp(x), Comp(z));
        public ValVector xw => new ValVector(Comp(x), Comp(w));
        public ValVector yx => new ValVector(Comp(y), Comp(x));
        public ValVector yy => new ValVector(Comp(y), Comp(y));
        public ValVector yz => new ValVector(Comp(y), Comp(z));
        public ValVector yw => new ValVector(Comp(y), Comp(w));
        public ValVector zx => new ValVector(Comp(z), Comp(x));
        public ValVector zy => new ValVector(Comp(z), Comp(y));
        public ValVector zz => new ValVector(Comp(z), Comp(z));
        public ValVector zw => new ValVector(Comp(z), Comp(w));
        public ValVector wx => new ValVector(Comp(w), Comp(x));
        public ValVector wy => new ValVector(Comp(w), Comp(y));
        public ValVector wz => new ValVector(Comp(w), Comp(z));
        public ValVector ww => new ValVector(Comp(w), Comp(w));

        #endregion

        #region xxx

        public ValVector xxx => new ValVector(Comp(x), Comp(x), Comp(x));
        public ValVector xxy => new ValVector(Comp(x), Comp(x), Comp(y));
        public ValVector xxz => new ValVector(Comp(x), Comp(x), Comp(z));
        public ValVector xxw => new ValVector(Comp(x), Comp(x), Comp(w));
        public ValVector xyx => new ValVector(Comp(x), Comp(y), Comp(x));
        public ValVector xyy => new ValVector(Comp(x), Comp(y), Comp(y));
        public ValVector xyz => new ValVector(Comp(x), Comp(y), Comp(z));
        public ValVector xyw => new ValVector(Comp(x), Comp(y), Comp(w));
        public ValVector xzx => new ValVector(Comp(x), Comp(z), Comp(x));
        public ValVector xzy => new ValVector(Comp(x), Comp(z), Comp(y));
        public ValVector xzz => new ValVector(Comp(x), Comp(z), Comp(z));
        public ValVector xzw => new ValVector(Comp(x), Comp(z), Comp(w));
        public ValVector xwx => new ValVector(Comp(x), Comp(w), Comp(x));
        public ValVector xwy => new ValVector(Comp(x), Comp(w), Comp(y));
        public ValVector xwz => new ValVector(Comp(x), Comp(w), Comp(z));
        public ValVector xww => new ValVector(Comp(x), Comp(w), Comp(w));
        public ValVector yxx => new ValVector(Comp(y), Comp(x), Comp(x));
        public ValVector yxy => new ValVector(Comp(y), Comp(x), Comp(y));
        public ValVector yxz => new ValVector(Comp(y), Comp(x), Comp(z));
        public ValVector yxw => new ValVector(Comp(y), Comp(x), Comp(w));
        public ValVector yyx => new ValVector(Comp(y), Comp(y), Comp(x));
        public ValVector yyy => new ValVector(Comp(y), Comp(y), Comp(y));
        public ValVector yyz => new ValVector(Comp(y), Comp(y), Comp(z));
        public ValVector yyw => new ValVector(Comp(y), Comp(y), Comp(w));
        public ValVector yzx => new ValVector(Comp(y), Comp(z), Comp(x));
        public ValVector yzy => new ValVector(Comp(y), Comp(z), Comp(y));
        public ValVector yzz => new ValVector(Comp(y), Comp(z), Comp(z));
        public ValVector yzw => new ValVector(Comp(y), Comp(z), Comp(w));
        public ValVector ywx => new ValVector(Comp(y), Comp(w), Comp(x));
        public ValVector ywy => new ValVector(Comp(y), Comp(w), Comp(y));
        public ValVector ywz => new ValVector(Comp(y), Comp(w), Comp(z));
        public ValVector yww => new ValVector(Comp(y), Comp(w), Comp(w));
        public ValVector zxx => new ValVector(Comp(z), Comp(x), Comp(x));
        public ValVector zxy => new ValVector(Comp(z), Comp(x), Comp(y));
        public ValVector zxz => new ValVector(Comp(z), Comp(x), Comp(z));
        public ValVector zxw => new ValVector(Comp(z), Comp(x), Comp(w));
        public ValVector zyx => new ValVector(Comp(z), Comp(y), Comp(x));
        public ValVector zyy => new ValVector(Comp(z), Comp(y), Comp(y));
        public ValVector zyz => new ValVector(Comp(z), Comp(y), Comp(z));
        public ValVector zyw => new ValVector(Comp(z), Comp(y), Comp(w));
        public ValVector zzx => new ValVector(Comp(z), Comp(z), Comp(x));
        public ValVector zzy => new ValVector(Comp(z), Comp(z), Comp(y));
        public ValVector zzz => new ValVector(Comp(z), Comp(z), Comp(z));
        public ValVector zzw => new ValVector(Comp(z), Comp(z), Comp(w));
        public ValVector zwx => new ValVector(Comp(z), Comp(w), Comp(x));
        public ValVector zwy => new ValVector(Comp(z), Comp(w), Comp(y));
        public ValVector zwz => new ValVector(Comp(z), Comp(w), Comp(z));
        public ValVector zww => new ValVector(Comp(z), Comp(w), Comp(w));
        public ValVector wxx => new ValVector(Comp(w), Comp(x), Comp(x));
        public ValVector wxy => new ValVector(Comp(w), Comp(x), Comp(y));
        public ValVector wxz => new ValVector(Comp(w), Comp(x), Comp(z));
        public ValVector wxw => new ValVector(Comp(w), Comp(x), Comp(w));
        public ValVector wyx => new ValVector(Comp(w), Comp(y), Comp(x));
        public ValVector wyy => new ValVector(Comp(w), Comp(y), Comp(y));
        public ValVector wyz => new ValVector(Comp(w), Comp(y), Comp(z));
        public ValVector wyw => new ValVector(Comp(w), Comp(y), Comp(w));
        public ValVector wzx => new ValVector(Comp(w), Comp(z), Comp(x));
        public ValVector wzy => new ValVector(Comp(w), Comp(z), Comp(y));
        public ValVector wzz => new ValVector(Comp(w), Comp(z), Comp(z));
        public ValVector wzw => new ValVector(Comp(w), Comp(z), Comp(w));
        public ValVector wwx => new ValVector(Comp(w), Comp(w), Comp(x));
        public ValVector wwy => new ValVector(Comp(w), Comp(w), Comp(y));
        public ValVector wwz => new ValVector(Comp(w), Comp(w), Comp(z));
        public ValVector www => new ValVector(Comp(w), Comp(w), Comp(w));

        #endregion

        #region xxxx

        public ValVector xxxx => new ValVector(Comp(x), Comp(x), Comp(x), Comp(x));
        public ValVector xxxy => new ValVector(Comp(x), Comp(x), Comp(x), Comp(y));
        public ValVector xxxz => new ValVector(Comp(x), Comp(x), Comp(x), Comp(z));
        public ValVector xxxw => new ValVector(Comp(x), Comp(x), Comp(x), Comp(w));
        public ValVector xxyx => new ValVector(Comp(x), Comp(x), Comp(y), Comp(x));
        public ValVector xxyy => new ValVector(Comp(x), Comp(x), Comp(y), Comp(y));
        public ValVector xxyz => new ValVector(Comp(x), Comp(x), Comp(y), Comp(z));
        public ValVector xxyw => new ValVector(Comp(x), Comp(x), Comp(y), Comp(w));
        public ValVector xxzx => new ValVector(Comp(x), Comp(x), Comp(z), Comp(x));
        public ValVector xxzy => new ValVector(Comp(x), Comp(x), Comp(z), Comp(y));
        public ValVector xxzz => new ValVector(Comp(x), Comp(x), Comp(z), Comp(z));
        public ValVector xxzw => new ValVector(Comp(x), Comp(x), Comp(z), Comp(w));
        public ValVector xxwx => new ValVector(Comp(x), Comp(x), Comp(w), Comp(x));
        public ValVector xxwy => new ValVector(Comp(x), Comp(x), Comp(w), Comp(y));
        public ValVector xxwz => new ValVector(Comp(x), Comp(x), Comp(w), Comp(z));
        public ValVector xxww => new ValVector(Comp(x), Comp(x), Comp(w), Comp(w));
        public ValVector xyxx => new ValVector(Comp(x), Comp(y), Comp(x), Comp(x));
        public ValVector xyxy => new ValVector(Comp(x), Comp(y), Comp(x), Comp(y));
        public ValVector xyxz => new ValVector(Comp(x), Comp(y), Comp(x), Comp(z));
        public ValVector xyxw => new ValVector(Comp(x), Comp(y), Comp(x), Comp(w));
        public ValVector xyyx => new ValVector(Comp(x), Comp(y), Comp(y), Comp(x));
        public ValVector xyyy => new ValVector(Comp(x), Comp(y), Comp(y), Comp(y));
        public ValVector xyyz => new ValVector(Comp(x), Comp(y), Comp(y), Comp(z));
        public ValVector xyyw => new ValVector(Comp(x), Comp(y), Comp(y), Comp(w));
        public ValVector xyzx => new ValVector(Comp(x), Comp(y), Comp(z), Comp(x));
        public ValVector xyzy => new ValVector(Comp(x), Comp(y), Comp(z), Comp(y));
        public ValVector xyzz => new ValVector(Comp(x), Comp(y), Comp(z), Comp(z));
        public ValVector xyzw => new ValVector(Comp(x), Comp(y), Comp(w), Comp(w));
        public ValVector xywx => new ValVector(Comp(x), Comp(y), Comp(w), Comp(x));
        public ValVector xywy => new ValVector(Comp(x), Comp(y), Comp(w), Comp(y));
        public ValVector xywz => new ValVector(Comp(x), Comp(y), Comp(w), Comp(z));
        public ValVector xyww => new ValVector(Comp(x), Comp(y), Comp(w), Comp(w));
        public ValVector xzxx => new ValVector(Comp(x), Comp(z), Comp(x), Comp(x));
        public ValVector xzxy => new ValVector(Comp(x), Comp(z), Comp(x), Comp(y));
        public ValVector xzxz => new ValVector(Comp(x), Comp(z), Comp(x), Comp(z));
        public ValVector xzxw => new ValVector(Comp(x), Comp(z), Comp(x), Comp(w));
        public ValVector xzyx => new ValVector(Comp(x), Comp(z), Comp(y), Comp(x));
        public ValVector xzyy => new ValVector(Comp(x), Comp(z), Comp(y), Comp(y));
        public ValVector xzyz => new ValVector(Comp(x), Comp(z), Comp(y), Comp(z));
        public ValVector xzyw => new ValVector(Comp(x), Comp(z), Comp(y), Comp(w));
        public ValVector xzzx => new ValVector(Comp(x), Comp(z), Comp(z), Comp(x));
        public ValVector xzzy => new ValVector(Comp(x), Comp(z), Comp(z), Comp(y));
        public ValVector xzzz => new ValVector(Comp(x), Comp(z), Comp(z), Comp(z));
        public ValVector xzzw => new ValVector(Comp(x), Comp(z), Comp(z), Comp(w));
        public ValVector xzwx => new ValVector(Comp(x), Comp(z), Comp(w), Comp(x));
        public ValVector xzwy => new ValVector(Comp(x), Comp(z), Comp(w), Comp(y));
        public ValVector xzwz => new ValVector(Comp(x), Comp(z), Comp(w), Comp(z));
        public ValVector xzww => new ValVector(Comp(x), Comp(z), Comp(w), Comp(w));
        public ValVector xwxx => new ValVector(Comp(x), Comp(w), Comp(x), Comp(x));
        public ValVector xwxy => new ValVector(Comp(x), Comp(w), Comp(x), Comp(y));
        public ValVector xwxz => new ValVector(Comp(x), Comp(w), Comp(x), Comp(z));
        public ValVector xwxw => new ValVector(Comp(x), Comp(w), Comp(x), Comp(w));
        public ValVector xwyx => new ValVector(Comp(x), Comp(w), Comp(y), Comp(x));
        public ValVector xwyy => new ValVector(Comp(x), Comp(w), Comp(y), Comp(y));
        public ValVector xwyz => new ValVector(Comp(x), Comp(w), Comp(y), Comp(z));
        public ValVector xwyw => new ValVector(Comp(x), Comp(w), Comp(y), Comp(w));
        public ValVector xwzx => new ValVector(Comp(x), Comp(w), Comp(z), Comp(x));
        public ValVector xwzy => new ValVector(Comp(x), Comp(w), Comp(z), Comp(y));
        public ValVector xwzz => new ValVector(Comp(x), Comp(w), Comp(z), Comp(z));
        public ValVector xwzw => new ValVector(Comp(x), Comp(w), Comp(z), Comp(w));
        public ValVector xwwx => new ValVector(Comp(x), Comp(w), Comp(w), Comp(x));
        public ValVector xwwy => new ValVector(Comp(x), Comp(w), Comp(w), Comp(y));
        public ValVector xwwz => new ValVector(Comp(x), Comp(w), Comp(w), Comp(z));
        public ValVector xwww => new ValVector(Comp(x), Comp(w), Comp(w), Comp(w));
        public ValVector yxxx => new ValVector(Comp(y), Comp(x), Comp(x), Comp(x));
        public ValVector yxxy => new ValVector(Comp(y), Comp(x), Comp(x), Comp(y));
        public ValVector yxxz => new ValVector(Comp(y), Comp(x), Comp(x), Comp(z));
        public ValVector yxxw => new ValVector(Comp(y), Comp(x), Comp(x), Comp(w));
        public ValVector yxyx => new ValVector(Comp(y), Comp(x), Comp(y), Comp(x));
        public ValVector yxyy => new ValVector(Comp(y), Comp(x), Comp(y), Comp(y));
        public ValVector yxyz => new ValVector(Comp(y), Comp(x), Comp(y), Comp(z));
        public ValVector yxyw => new ValVector(Comp(y), Comp(x), Comp(y), Comp(w));
        public ValVector yxzx => new ValVector(Comp(y), Comp(x), Comp(z), Comp(x));
        public ValVector yxzy => new ValVector(Comp(y), Comp(x), Comp(z), Comp(y));
        public ValVector yxzz => new ValVector(Comp(y), Comp(x), Comp(z), Comp(z));
        public ValVector yxzw => new ValVector(Comp(y), Comp(x), Comp(z), Comp(w));
        public ValVector yxwx => new ValVector(Comp(y), Comp(x), Comp(w), Comp(x));
        public ValVector yxwy => new ValVector(Comp(y), Comp(x), Comp(w), Comp(y));
        public ValVector yxwz => new ValVector(Comp(y), Comp(x), Comp(w), Comp(z));
        public ValVector yxww => new ValVector(Comp(y), Comp(x), Comp(w), Comp(w));
        public ValVector yyxx => new ValVector(Comp(y), Comp(y), Comp(x), Comp(x));
        public ValVector yyxy => new ValVector(Comp(y), Comp(y), Comp(x), Comp(y));
        public ValVector yyxz => new ValVector(Comp(y), Comp(y), Comp(x), Comp(z));
        public ValVector yyxw => new ValVector(Comp(y), Comp(y), Comp(x), Comp(w));
        public ValVector yyyx => new ValVector(Comp(y), Comp(y), Comp(y), Comp(x));
        public ValVector yyyy => new ValVector(Comp(y), Comp(y), Comp(y), Comp(y));
        public ValVector yyyz => new ValVector(Comp(y), Comp(y), Comp(y), Comp(z));
        public ValVector yyyw => new ValVector(Comp(y), Comp(y), Comp(y), Comp(w));
        public ValVector yyzx => new ValVector(Comp(y), Comp(y), Comp(z), Comp(x));
        public ValVector yyzy => new ValVector(Comp(y), Comp(y), Comp(z), Comp(y));
        public ValVector yyzz => new ValVector(Comp(y), Comp(y), Comp(z), Comp(z));
        public ValVector yyzw => new ValVector(Comp(y), Comp(y), Comp(z), Comp(w));
        public ValVector yywx => new ValVector(Comp(y), Comp(y), Comp(w), Comp(x));
        public ValVector yywy => new ValVector(Comp(y), Comp(y), Comp(w), Comp(y));
        public ValVector yywz => new ValVector(Comp(y), Comp(y), Comp(w), Comp(z));
        public ValVector yyww => new ValVector(Comp(y), Comp(y), Comp(w), Comp(w));
        public ValVector yzxx => new ValVector(Comp(y), Comp(z), Comp(x), Comp(x));
        public ValVector yzxy => new ValVector(Comp(y), Comp(z), Comp(x), Comp(y));
        public ValVector yzxz => new ValVector(Comp(y), Comp(z), Comp(x), Comp(z));
        public ValVector yzxw => new ValVector(Comp(y), Comp(z), Comp(x), Comp(w));
        public ValVector yzyx => new ValVector(Comp(y), Comp(z), Comp(y), Comp(x));
        public ValVector yzyy => new ValVector(Comp(y), Comp(z), Comp(y), Comp(y));
        public ValVector yzyz => new ValVector(Comp(y), Comp(z), Comp(y), Comp(z));
        public ValVector yzyw => new ValVector(Comp(y), Comp(z), Comp(y), Comp(w));
        public ValVector yzzx => new ValVector(Comp(y), Comp(z), Comp(z), Comp(x));
        public ValVector yzzy => new ValVector(Comp(y), Comp(z), Comp(z), Comp(y));
        public ValVector yzzz => new ValVector(Comp(y), Comp(z), Comp(z), Comp(z));
        public ValVector yzzw => new ValVector(Comp(y), Comp(z), Comp(z), Comp(w));
        public ValVector yzwx => new ValVector(Comp(y), Comp(z), Comp(w), Comp(x));
        public ValVector yzwy => new ValVector(Comp(y), Comp(z), Comp(w), Comp(y));
        public ValVector yzwz => new ValVector(Comp(y), Comp(z), Comp(w), Comp(z));
        public ValVector yzww => new ValVector(Comp(y), Comp(z), Comp(w), Comp(w));
        public ValVector ywxx => new ValVector(Comp(y), Comp(w), Comp(x), Comp(x));
        public ValVector ywxy => new ValVector(Comp(y), Comp(w), Comp(x), Comp(y));
        public ValVector ywxz => new ValVector(Comp(y), Comp(w), Comp(x), Comp(z));
        public ValVector ywxw => new ValVector(Comp(y), Comp(w), Comp(x), Comp(w));
        public ValVector ywyx => new ValVector(Comp(y), Comp(w), Comp(y), Comp(x));
        public ValVector ywyy => new ValVector(Comp(y), Comp(w), Comp(y), Comp(y));
        public ValVector ywyz => new ValVector(Comp(y), Comp(w), Comp(y), Comp(z));
        public ValVector ywyw => new ValVector(Comp(y), Comp(w), Comp(y), Comp(w));
        public ValVector ywzx => new ValVector(Comp(y), Comp(w), Comp(z), Comp(x));
        public ValVector ywzy => new ValVector(Comp(y), Comp(w), Comp(z), Comp(y));
        public ValVector ywzz => new ValVector(Comp(y), Comp(w), Comp(z), Comp(z));
        public ValVector ywzw => new ValVector(Comp(y), Comp(w), Comp(z), Comp(w));
        public ValVector ywwx => new ValVector(Comp(y), Comp(w), Comp(w), Comp(x));
        public ValVector ywwy => new ValVector(Comp(y), Comp(w), Comp(w), Comp(y));
        public ValVector ywwz => new ValVector(Comp(y), Comp(w), Comp(w), Comp(z));
        public ValVector ywww => new ValVector(Comp(y), Comp(w), Comp(w), Comp(w));
        public ValVector zxxx => new ValVector(Comp(z), Comp(x), Comp(x), Comp(x));
        public ValVector zxxy => new ValVector(Comp(z), Comp(x), Comp(x), Comp(y));
        public ValVector zxxz => new ValVector(Comp(z), Comp(x), Comp(x), Comp(z));
        public ValVector zxxw => new ValVector(Comp(z), Comp(x), Comp(x), Comp(w));
        public ValVector zxyx => new ValVector(Comp(z), Comp(x), Comp(y), Comp(x));
        public ValVector zxyy => new ValVector(Comp(z), Comp(x), Comp(y), Comp(y));
        public ValVector zxyz => new ValVector(Comp(z), Comp(x), Comp(y), Comp(z));
        public ValVector zxyw => new ValVector(Comp(z), Comp(x), Comp(y), Comp(w));
        public ValVector zxzx => new ValVector(Comp(z), Comp(x), Comp(z), Comp(x));
        public ValVector zxzy => new ValVector(Comp(z), Comp(x), Comp(z), Comp(y));
        public ValVector zxzz => new ValVector(Comp(z), Comp(x), Comp(z), Comp(z));
        public ValVector zxzw => new ValVector(Comp(z), Comp(x), Comp(z), Comp(w));
        public ValVector zxwx => new ValVector(Comp(z), Comp(x), Comp(w), Comp(x));
        public ValVector zxwy => new ValVector(Comp(z), Comp(x), Comp(w), Comp(y));
        public ValVector zxwz => new ValVector(Comp(z), Comp(x), Comp(w), Comp(z));
        public ValVector zxww => new ValVector(Comp(z), Comp(x), Comp(w), Comp(w));
        public ValVector zyxx => new ValVector(Comp(z), Comp(y), Comp(x), Comp(x));
        public ValVector zyxy => new ValVector(Comp(z), Comp(y), Comp(x), Comp(y));
        public ValVector zyxz => new ValVector(Comp(z), Comp(y), Comp(x), Comp(z));
        public ValVector zyxw => new ValVector(Comp(z), Comp(y), Comp(x), Comp(w));
        public ValVector zyyx => new ValVector(Comp(z), Comp(y), Comp(y), Comp(x));
        public ValVector zyyy => new ValVector(Comp(z), Comp(y), Comp(y), Comp(y));
        public ValVector zyyz => new ValVector(Comp(z), Comp(y), Comp(y), Comp(z));
        public ValVector zyyw => new ValVector(Comp(z), Comp(y), Comp(y), Comp(w));
        public ValVector zyzx => new ValVector(Comp(z), Comp(y), Comp(z), Comp(x));
        public ValVector zyzy => new ValVector(Comp(z), Comp(y), Comp(z), Comp(y));
        public ValVector zyzz => new ValVector(Comp(z), Comp(y), Comp(z), Comp(z));
        public ValVector zyzw => new ValVector(Comp(z), Comp(y), Comp(z), Comp(w));
        public ValVector zywx => new ValVector(Comp(z), Comp(y), Comp(w), Comp(x));
        public ValVector zywy => new ValVector(Comp(z), Comp(y), Comp(w), Comp(y));
        public ValVector zywz => new ValVector(Comp(z), Comp(y), Comp(w), Comp(z));
        public ValVector zyww => new ValVector(Comp(z), Comp(y), Comp(w), Comp(w));
        public ValVector zzxx => new ValVector(Comp(z), Comp(z), Comp(x), Comp(x));
        public ValVector zzxy => new ValVector(Comp(z), Comp(z), Comp(x), Comp(y));
        public ValVector zzxz => new ValVector(Comp(z), Comp(z), Comp(x), Comp(z));
        public ValVector zzxw => new ValVector(Comp(z), Comp(z), Comp(x), Comp(w));
        public ValVector zzyx => new ValVector(Comp(z), Comp(z), Comp(y), Comp(x));
        public ValVector zzyy => new ValVector(Comp(z), Comp(z), Comp(y), Comp(y));
        public ValVector zzyz => new ValVector(Comp(z), Comp(z), Comp(y), Comp(z));
        public ValVector zzyw => new ValVector(Comp(z), Comp(z), Comp(y), Comp(w));
        public ValVector zzzx => new ValVector(Comp(z), Comp(z), Comp(z), Comp(x));
        public ValVector zzzy => new ValVector(Comp(z), Comp(z), Comp(z), Comp(y));
        public ValVector zzzz => new ValVector(Comp(z), Comp(z), Comp(z), Comp(z));
        public ValVector zzzw => new ValVector(Comp(z), Comp(z), Comp(z), Comp(w));
        public ValVector zzwx => new ValVector(Comp(z), Comp(z), Comp(w), Comp(x));
        public ValVector zzwy => new ValVector(Comp(z), Comp(z), Comp(w), Comp(y));
        public ValVector zzwz => new ValVector(Comp(z), Comp(z), Comp(w), Comp(z));
        public ValVector zzww => new ValVector(Comp(z), Comp(z), Comp(w), Comp(w));
        public ValVector zwxx => new ValVector(Comp(z), Comp(w), Comp(x), Comp(x));
        public ValVector zwxy => new ValVector(Comp(z), Comp(w), Comp(x), Comp(y));
        public ValVector zwxz => new ValVector(Comp(z), Comp(w), Comp(x), Comp(z));
        public ValVector zwxw => new ValVector(Comp(z), Comp(w), Comp(x), Comp(w));
        public ValVector zwyx => new ValVector(Comp(z), Comp(w), Comp(y), Comp(x));
        public ValVector zwyy => new ValVector(Comp(z), Comp(w), Comp(y), Comp(y));
        public ValVector zwyz => new ValVector(Comp(z), Comp(w), Comp(y), Comp(z));
        public ValVector zwyw => new ValVector(Comp(z), Comp(w), Comp(y), Comp(w));
        public ValVector zwzx => new ValVector(Comp(z), Comp(w), Comp(z), Comp(x));
        public ValVector zwzy => new ValVector(Comp(z), Comp(w), Comp(z), Comp(y));
        public ValVector zwzz => new ValVector(Comp(z), Comp(w), Comp(z), Comp(z));
        public ValVector zwzw => new ValVector(Comp(z), Comp(w), Comp(z), Comp(w));
        public ValVector zwwx => new ValVector(Comp(z), Comp(w), Comp(w), Comp(x));
        public ValVector zwwy => new ValVector(Comp(z), Comp(w), Comp(w), Comp(y));
        public ValVector zwwz => new ValVector(Comp(z), Comp(w), Comp(w), Comp(z));
        public ValVector zwww => new ValVector(Comp(z), Comp(w), Comp(w), Comp(w));
        public ValVector wxxx => new ValVector(Comp(w), Comp(x), Comp(x), Comp(x));
        public ValVector wxxy => new ValVector(Comp(w), Comp(x), Comp(x), Comp(y));
        public ValVector wxxz => new ValVector(Comp(w), Comp(x), Comp(x), Comp(z));
        public ValVector wxxw => new ValVector(Comp(w), Comp(x), Comp(x), Comp(w));
        public ValVector wxyx => new ValVector(Comp(w), Comp(x), Comp(y), Comp(x));
        public ValVector wxyy => new ValVector(Comp(w), Comp(x), Comp(y), Comp(y));
        public ValVector wxyz => new ValVector(Comp(w), Comp(x), Comp(y), Comp(z));
        public ValVector wxyw => new ValVector(Comp(w), Comp(x), Comp(y), Comp(w));
        public ValVector wxzx => new ValVector(Comp(w), Comp(x), Comp(z), Comp(x));
        public ValVector wxzy => new ValVector(Comp(w), Comp(x), Comp(z), Comp(y));
        public ValVector wxzz => new ValVector(Comp(w), Comp(x), Comp(z), Comp(z));
        public ValVector wxzw => new ValVector(Comp(w), Comp(x), Comp(z), Comp(w));
        public ValVector wxwx => new ValVector(Comp(w), Comp(x), Comp(w), Comp(x));
        public ValVector wxwy => new ValVector(Comp(w), Comp(x), Comp(w), Comp(y));
        public ValVector wxwz => new ValVector(Comp(w), Comp(x), Comp(w), Comp(z));
        public ValVector wxww => new ValVector(Comp(w), Comp(x), Comp(w), Comp(w));
        public ValVector wyxx => new ValVector(Comp(w), Comp(y), Comp(x), Comp(x));
        public ValVector wyxy => new ValVector(Comp(w), Comp(y), Comp(x), Comp(y));
        public ValVector wyxz => new ValVector(Comp(w), Comp(y), Comp(x), Comp(z));
        public ValVector wyxw => new ValVector(Comp(w), Comp(y), Comp(x), Comp(w));
        public ValVector wyyx => new ValVector(Comp(w), Comp(y), Comp(y), Comp(x));
        public ValVector wyyy => new ValVector(Comp(w), Comp(y), Comp(y), Comp(y));
        public ValVector wyyz => new ValVector(Comp(w), Comp(y), Comp(y), Comp(z));
        public ValVector wyyw => new ValVector(Comp(w), Comp(y), Comp(y), Comp(w));
        public ValVector wyzx => new ValVector(Comp(w), Comp(y), Comp(z), Comp(x));
        public ValVector wyzy => new ValVector(Comp(w), Comp(y), Comp(z), Comp(y));
        public ValVector wyzz => new ValVector(Comp(w), Comp(y), Comp(z), Comp(z));
        public ValVector wyzw => new ValVector(Comp(w), Comp(y), Comp(z), Comp(w));
        public ValVector wywx => new ValVector(Comp(w), Comp(y), Comp(w), Comp(x));
        public ValVector wywy => new ValVector(Comp(w), Comp(y), Comp(w), Comp(y));
        public ValVector wywz => new ValVector(Comp(w), Comp(y), Comp(w), Comp(z));
        public ValVector wyww => new ValVector(Comp(w), Comp(y), Comp(w), Comp(w));
        public ValVector wzxx => new ValVector(Comp(w), Comp(z), Comp(x), Comp(x));
        public ValVector wzxy => new ValVector(Comp(w), Comp(z), Comp(x), Comp(y));
        public ValVector wzxz => new ValVector(Comp(w), Comp(z), Comp(x), Comp(z));
        public ValVector wzxw => new ValVector(Comp(w), Comp(z), Comp(x), Comp(w));
        public ValVector wzyx => new ValVector(Comp(w), Comp(z), Comp(y), Comp(x));
        public ValVector wzyy => new ValVector(Comp(w), Comp(z), Comp(y), Comp(y));
        public ValVector wzyz => new ValVector(Comp(w), Comp(z), Comp(y), Comp(z));
        public ValVector wzyw => new ValVector(Comp(w), Comp(z), Comp(y), Comp(w));
        public ValVector wzzx => new ValVector(Comp(w), Comp(z), Comp(z), Comp(x));
        public ValVector wzzy => new ValVector(Comp(w), Comp(z), Comp(z), Comp(y));
        public ValVector wzzz => new ValVector(Comp(w), Comp(z), Comp(z), Comp(z));
        public ValVector wzzw => new ValVector(Comp(w), Comp(z), Comp(z), Comp(w));
        public ValVector wzwx => new ValVector(Comp(w), Comp(z), Comp(w), Comp(x));
        public ValVector wzwy => new ValVector(Comp(w), Comp(z), Comp(w), Comp(y));
        public ValVector wzwz => new ValVector(Comp(w), Comp(z), Comp(w), Comp(z));
        public ValVector wzww => new ValVector(Comp(w), Comp(z), Comp(w), Comp(w));
        public ValVector wwxx => new ValVector(Comp(w), Comp(w), Comp(x), Comp(x));
        public ValVector wwxy => new ValVector(Comp(w), Comp(w), Comp(x), Comp(y));
        public ValVector wwxz => new ValVector(Comp(w), Comp(w), Comp(x), Comp(z));
        public ValVector wwxw => new ValVector(Comp(w), Comp(w), Comp(x), Comp(w));
        public ValVector wwyx => new ValVector(Comp(w), Comp(w), Comp(y), Comp(x));
        public ValVector wwyy => new ValVector(Comp(w), Comp(w), Comp(y), Comp(y));
        public ValVector wwyz => new ValVector(Comp(w), Comp(w), Comp(y), Comp(z));
        public ValVector wwyw => new ValVector(Comp(w), Comp(w), Comp(y), Comp(w));
        public ValVector wwzx => new ValVector(Comp(w), Comp(w), Comp(z), Comp(x));
        public ValVector wwzy => new ValVector(Comp(w), Comp(w), Comp(z), Comp(y));
        public ValVector wwzz => new ValVector(Comp(w), Comp(w), Comp(z), Comp(z));
        public ValVector wwzw => new ValVector(Comp(w), Comp(w), Comp(z), Comp(w));
        public ValVector wwwx => new ValVector(Comp(w), Comp(w), Comp(w), Comp(x));
        public ValVector wwwy => new ValVector(Comp(w), Comp(w), Comp(w), Comp(y));
        public ValVector wwwz => new ValVector(Comp(w), Comp(w), Comp(w), Comp(z));
        public ValVector wwww => new ValVector(Comp(w), Comp(w), Comp(w), Comp(w));

        #endregion

        #endregion

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

    /// <summary>
    /// ValString represents a string (text) value.
    /// </summary>
    public class ValString : Value {
		public static long maxSize = 0xFFFFFF;		// about 16M elements
		
		public string value;

		public ValString(string value) {
			this.value = value ?? _empty.value;
		}

		public override string ToString(TAC.Machine vm) {
			return value;
		}

		public override string CodeForm(TAC.Machine vm, int recursionLimit=-1) {
			return "\"" + value.Replace("\"", "\"\"") + "\"";
		}

		public override bool BoolValue() {
			// Any nonempty string is considered true.
			return !string.IsNullOrEmpty(value);
		}

		public override bool IsA(Value type, TAC.Machine vm) {
			return type == vm.stringType;
		}

		public override int Hash() {
			return value.GetHashCode();
		}

		public override double Equality(Value rhs) {
			// String equality is treated the same as in C#.
			return rhs is ValString && ((ValString)rhs).value == value ? 1 : 0;
		}

		public Value GetElem(Value index) {
			if (!(index is ValNumber)) throw new KeyException("String index must be numeric", null);
			var i = index.IntValue();
			if (i < 0) i += value.Length;
			if (i < 0 || i >= value.Length) {
				throw new IndexException("Index Error (string index " + index + " out of range)");

			}
			return new ValString(value.Substring(i, 1));
		}

		// Magic identifier for the is-a entry in the class system:
		public static ValString magicIsA = new ValString("__isa");
		
		static ValString _empty = new ValString("");
		
		/// <summary>
		/// Handy accessor for an empty ValString.
		/// IMPORTANT: do not alter the value of the object returned!
		/// </summary>
		public static ValString empty { get { return _empty; } }

	}
	
	// We frequently need to generate a ValString out of a string for fleeting purposes,
	// like looking up an identifier in a map (which we do ALL THE TIME).  So, here's
	// a little recycling pool of reusable ValStrings, for this purpose only.
	class TempValString : ValString {
		private TempValString next;

		private TempValString(string s) : base(s) {
			this.next = null;
		}

		private static TempValString _tempPoolHead = null;
		private static object lockObj = new object();
		public static TempValString Get(string s) {
			lock(lockObj) {
				if (_tempPoolHead == null) {
					return new TempValString(s);
				} else {
					var result = _tempPoolHead;
					_tempPoolHead = _tempPoolHead.next;
					result.value = s;
					return result;
				}
			}
		}
		public static void Release(TempValString temp) {
			lock(lockObj) {
				temp.next = _tempPoolHead;
				_tempPoolHead = temp;
			}
		}
	}
	
	
	/// <summary>
	/// ValList represents a MiniScript list (which, under the hood, is
	/// just a wrapper for a List of Values).
	/// </summary>
	public class ValList : Value {
		public static long maxSize = 0xFFFFFF;		// about 16 MB
		
		public List<Value> values;

		public ValList(List<Value> values = null) {
			this.values = values == null ? new List<Value>() : values;
		}

		public override Value FullEval(TAC.Context context) {
			// Evaluate each of our list elements, and if any of those is
			// a variable or temp, then resolve those now.
			// CAUTION: do not mutate our original list!  We may need
			// it in its original form on future iterations.
			ValList result = null;
			for (var i = 0; i < values.Count; i++) {
				var copied = false;
				if (values[i] is ValTemp || values[i] is ValVar) {
					Value newVal = values[i].Val(context);
					if (newVal != values[i]) {
						// OK, something changed, so we're going to need a new copy of the list.
						if (result == null) {
							result = new ValList();
							for (var j = 0; j < i; j++) result.values.Add(values[j]);
						}
						result.values.Add(newVal);
						copied = true;
					}
				}
				if (!copied && result != null) {
					// No change; but we have new results to return, so copy it as-is
					result.values.Add(values[i]);
				}
			}
			return result ?? this;
		}

		public ValList EvalCopy(TAC.Context context) {
			// Create a copy of this list, evaluating its members as we go.
			// This is used when a list literal appears in the source, to
			// ensure that each time that code executes, we get a new, distinct
			// mutable object, rather than the same object multiple times.
			var result = new ValList();
			for (var i = 0; i < values.Count; i++) {
				result.values.Add(values[i] == null ? null : values[i].Val(context));
			}
			return result;
		}

		public override string CodeForm(TAC.Machine vm, int recursionLimit=-1) {
			if (recursionLimit == 0) return "[...]";
			if (recursionLimit > 0 && recursionLimit < 3 && vm != null) {
				string shortName = vm.FindShortName(this);
				if (shortName != null) return shortName;
			}
			var strs = new string[values.Count];
			for (var i = 0; i < values.Count; i++) {
				if (values[i] == null) strs[i] = "null";
				else strs[i] = values[i].CodeForm(vm, recursionLimit - 1);
			}
			return "[" + string.Join(", ", strs) + "]";
		}

		public override string ToString(TAC.Machine vm) {
			return CodeForm(vm, 3);
		}

		public override bool BoolValue() {
			// A list is considered true if it is nonempty.
			return values != null && values.Count > 0;
		}

		public override bool IsA(Value type, TAC.Machine vm) {
			return type == vm.listType;
		}

		public override int Hash() {
			return RecursiveHash();
		}

		public override double Equality(Value rhs) {
			// Quick bail-out cases:
			if (!(rhs is ValList)) return 0;
			List<Value> rhl = ((ValList)rhs).values;
			if (rhl == values) return 1;  // (same list)
			int count = values.Count;
			if (count != rhl.Count) return 0;

			// Otherwise, we have to do:
			return RecursiveEqual(rhs) ? 1 : 0;
		}

		public override bool CanSetElem() { return true; }

		public override void SetElem(Value index, Value value) {
			var i = index.IntValue();
			if (i < 0) i += values.Count;
			if (i < 0 || i >= values.Count) {
				throw new IndexException("Index Error (list index " + index + " out of range)");
			}
			values[i] = value;
		}

		public Value GetElem(Value index) {
			if (!(index is ValNumber)) throw new KeyException("List index must be numeric", null);
			var i = index.IntValue();
			if (i < 0) i += values.Count;
			if (i < 0 || i >= values.Count) {
				throw new IndexException("Index Error (list index " + index + " out of range)");

			}
			return values[i];
		}

	}
	
	/// <summary>
	/// ValMap represents a MiniScript map, which under the hood is just a Dictionary
	/// of Value, Value pairs.
	/// </summary>
	public class ValMap : Value {

		// Define a maximum depth we will allow an inheritance ("__isa") chain to be.
		// This is used to avoid locking up the app if some bozo creates a loop in
		// the __isa chain, but it also means we can't allow actual inheritance trees
		// to be longer than this.  So, use a reasonably generous value.
		public const int maxIsaDepth = 256;

		public Dictionary<Value, Value> map;

		// Assignment override function: return true to cancel (override)
		// the assignment, or false to allow it to happen as normal.
		public delegate bool AssignOverrideFunc(Value key, Value value);
		public AssignOverrideFunc assignOverride;

		public ValMap() {
			this.map = new Dictionary<Value, Value>(RValueEqualityComparer.instance);
		}
		
		public override bool BoolValue() {
			// A map is considered true if it is nonempty.
			return map != null && map.Count > 0;
		}

		/// <summary>
		/// Convenience method to check whether the map contains a given string key.
		/// </summary>
		/// <param name="identifier">string key to check for</param>
		/// <returns>true if the map contains that key; false otherwise</returns>
		public bool ContainsKey(string identifier) {
			var idVal = TempValString.Get(identifier);
			bool result = map.ContainsKey(idVal);
			TempValString.Release(idVal);
			return result;
		}
		
		/// <summary>
		/// Convenience method to check whether this map contains a given key
		/// (of arbitrary type).
		/// </summary>
		/// <param name="key">key to check for</param>
		/// <returns>true if the map contains that key; false otherwise</returns>
		public bool ContainsKey(Value key) {
			if (key == null) key = ValNull.instance;
			return map.ContainsKey(key);
		}
		
		/// <summary>
		/// Get the number of entries in this map.
		/// </summary>
		public int Count {
			get { return map.Count; }
		}
		
		/// <summary>
		/// Return the KeyCollection for this map.
		/// </summary>
		public Dictionary<Value, Value>.KeyCollection Keys {
			get { return map.Keys; }
		}
		
		
		/// <summary>
		/// Accessor to get/set on element of this map by a string key, walking
		/// the __isa chain as needed.  (Note that if you want to avoid that, then
		/// simply look up your value in .map directly.)
		/// </summary>
		/// <param name="identifier">string key to get/set</param>
		/// <returns>value associated with that key</returns>
		public Value this [string identifier] {
			get { 
				var idVal = TempValString.Get(identifier);
				Value result = Lookup(idVal);
				TempValString.Release(idVal);
				return result;
			}
			set { map[new ValString(identifier)] = value; }
		}
		
		/// <summary>
		/// Look up the given identifier as quickly as possible, without
		/// walking the __isa chain or doing anything fancy.  (This is used
		/// when looking up local variables.)
		/// </summary>
		/// <param name="identifier">identifier to look up</param>
		/// <returns>true if found, false if not</returns>
		public bool TryGetValue(string identifier, out Value value) {
			if (map.Count < 5) {
				// new approach: just iterate!  This is faster for small maps (which are common).
				foreach (var kv in map) {
					if (kv.Key is ValString ks && ks.value == identifier) {
						value = kv.Value;
						return true;
					}
				}
				value = null;
				return false;
			}
			// old method, and still better on big maps: use dictionary look-up.
			var idVal = TempValString.Get(identifier);
			bool result = map.TryGetValue(idVal, out value);
			TempValString.Release(idVal);
			return result;
		}
		
		/// <summary>
		/// Look up a value in this dictionary, walking the __isa chain to find
		/// it in a parent object if necessary.  
		/// </summary>
		/// <param name="key">key to search for</param>
		/// <returns>value associated with that key, or null if not found</returns>
		public Value Lookup(Value key) {
			if (key == null) key = ValNull.instance;
			Value result = null;
			ValMap obj = this;
			int chainDepth = 0;
			while (obj != null) {
				if (obj.map.TryGetValue(key, out result)) return result;
				Value parent;
				if (!obj.map.TryGetValue(ValString.magicIsA, out parent)) break;
				if (chainDepth++ > maxIsaDepth) {
					throw new LimitExceededException("__isa depth exceeded (perhaps a reference loop?)");
				}
				obj = parent as ValMap;
			}
			return null;
		}
		
		/// <summary>
		/// Look up a value in this dictionary, walking the __isa chain to find
		/// it in a parent object if necessary; return both the value found and
		/// (via the output parameter) the map it was found in.
		/// </summary>
		/// <param name="key">key to search for</param>
		/// <returns>value associated with that key, or null if not found</returns>
		public Value Lookup(Value key, out ValMap valueFoundIn) {
			if (key == null) key = ValNull.instance;
			Value result = null;
			ValMap obj = this;
			int chainDepth = 0;
			while (obj != null) {
				if (obj.map.TryGetValue(key, out result)) {
					valueFoundIn = obj;
					return result;
				}
				Value parent;
				if (!obj.map.TryGetValue(ValString.magicIsA, out parent)) break;
				if (chainDepth++ > maxIsaDepth) {
					throw new LimitExceededException("__isa depth exceeded (perhaps a reference loop?)");
				}
				obj = parent as ValMap;
			}
			valueFoundIn = null;
			return null;
		}
		
		public override Value FullEval(TAC.Context context) {
			// Evaluate each of our elements, and if any of those is
			// a variable or temp, then resolve those now.
			foreach (Value k in map.Keys.ToArray()) {	// TODO: something more efficient here.
				Value key = k;		// stupid C#!
				Value value = map[key];
				if (key is ValTemp || key is ValVar) {
					map.Remove(key);
					key = key.Val(context);
					map[key] = value;
				}
				if (value is ValTemp || value is ValVar) {
					map[key] = value.Val(context);
				}
			}
			return this;
		}

		public ValMap EvalCopy(TAC.Context context) {
			// Create a copy of this map, evaluating its members as we go.
			// This is used when a map literal appears in the source, to
			// ensure that each time that code executes, we get a new, distinct
			// mutable object, rather than the same object multiple times.
			var result = new ValMap();
			foreach (Value k in map.Keys) {
				Value key = k;		// stupid C#!
				Value value = map[key];
				if (key is ValTemp || key is ValVar || value is ValSeqElem) key = key.Val(context);
				if (value is ValTemp || value is ValVar || value is ValSeqElem) value = value.Val(context);
				result.map[key] = value;
			}
			return result;
		}

		public override string CodeForm(TAC.Machine vm, int recursionLimit=-1) {
			if (recursionLimit == 0) return "{...}";
			if (recursionLimit > 0 && recursionLimit < 3 && vm != null) {
				string shortName = vm.FindShortName(this);
				if (shortName != null) return shortName;
			}
			var strs = new string[map.Count];
			int i = 0;
			foreach (KeyValuePair<Value, Value> kv in map) {
				int nextRecurLimit = recursionLimit - 1;
				if (kv.Key == ValString.magicIsA) nextRecurLimit = 1;
				strs[i++] = string.Format("{0}: {1}", kv.Key.CodeForm(vm, nextRecurLimit), 
					kv.Value == null ? "null" : kv.Value.CodeForm(vm, nextRecurLimit));
			}
			return "{" + String.Join(", ", strs) + "}";
		}

		public override string ToString(TAC.Machine vm) {
			return CodeForm(vm, 3);
		}

		public override bool IsA(Value type, TAC.Machine vm) {
			// If the given type is the magic 'map' type, then we're definitely
			// one of those.  Otherwise, we have to walk the __isa chain.
			if (type == vm.mapType) return true;
			Value p = null;
			map.TryGetValue(ValString.magicIsA, out p);
			int chainDepth = 0;
			while (p != null) {
				if (p == type) return true;
				if (!(p is ValMap)) return false;
				if (chainDepth++ > maxIsaDepth) {
					throw new LimitExceededException("__isa depth exceeded (perhaps a reference loop?)");
				}
				((ValMap)p).map.TryGetValue(ValString.magicIsA, out p);
			}
			return false;
		}

		public override int Hash() {
			return RecursiveHash();
		}

		public override double Equality(Value rhs) {
			// Quick bail-out cases:
			if (!(rhs is ValMap)) return 0;
			Dictionary<Value, Value> rhm = ((ValMap)rhs).map;
			if (rhm == map) return 1;  // (same map)
			int count = map.Count;
			if (count != rhm.Count) return 0;

			// Otherwise:
			return RecursiveEqual(rhs) ? 1 : 0;
		}

		public override bool CanSetElem() { return true; }

		/// <summary>
		/// Set the value associated with the given key (index).  This is where
		/// we take the opportunity to look for an assignment override function,
		/// and if found, give that a chance to handle it instead.
		/// </summary>
		public override void SetElem(Value index, Value value) {
			if (index == null) index = ValNull.instance;
			if (assignOverride == null || !assignOverride(index, value)) {
				map[index] = value;
			}
		}

		/// <summary>
		/// Get the indicated key/value pair as another map containing "key" and "value".
		/// (This is used when iterating over a map with "for".)
		/// </summary>
		/// <param name="index">0-based index of key/value pair to get.</param>
		/// <returns>new map containing "key" and "value" with the requested key/value pair</returns>
		public ValMap GetKeyValuePair(int index) {
			Dictionary<Value, Value>.KeyCollection keys = map.Keys;
			if (index < 0 || index >= keys.Count) {
				throw new IndexException("index " + index + " out of range for map");
			}
			Value key = keys.ElementAt<Value>(index);   // (TODO: consider more efficient methods here)
			var result = new ValMap();
			result.map[keyStr] = (key is ValNull ? null : key);
			result.map[valStr] = map[key];
			return result;
		}
		static ValString keyStr = new ValString("key");
		static ValString valStr = new ValString("value");

	}
	
	/// <summary>
	/// Function: our internal representation of a MiniScript function.  This includes
	/// its parameters and its code.  (It does not include a name -- functions don't 
	/// actually HAVE names; instead there are named variables whose value may happen 
	/// to be a function.)
	/// </summary>
	public class Function {
		/// <summary>
		/// Param: helper class representing a function parameter.
		/// </summary>
		public class Param {
			public string name;
			public Value defaultValue;

			public Param(string name, Value defaultValue) {
				this.name = name;
				this.defaultValue = defaultValue;
			}
		}
		
		// Function parameters
		public List<Param> parameters;
		
		// Function code (compiled down to TAC form)
		public List<TAC.Line> code;

		public Function(List<TAC.Line> code) {
			this.code = code;
			parameters = new List<Param>();
		}

		public string ToString(TAC.Machine vm) {
			var s = new System.Text.StringBuilder();
			s.Append("FUNCTION(");			
			for (var i=0; i < parameters.Count(); i++) {
				if (i > 0) s.Append(", ");
				s.Append(parameters[i].name);
				if (parameters[i].defaultValue != null) s.Append("=" + parameters[i].defaultValue.CodeForm(vm));
			}
			s.Append(")");
			return s.ToString();
		}
	}
	
	/// <summary>
	/// ValFunction: a Value that is, in fact, a Function.
	/// </summary>
	public class ValFunction : Value {
		public Function function;
		public readonly ValMap outerVars;	// local variables where the function was defined (usually, the module)

		public ValFunction(Function function) {
			this.function = function;
		}
		public ValFunction(Function function, ValMap outerVars) {
			this.function = function;
            this.outerVars = outerVars;
		}

		public override string ToString(TAC.Machine vm) {
			return function.ToString(vm);
		}

		public override bool BoolValue() {
			// A function value is ALWAYS considered true.
			return true;
		}

		public override bool IsA(Value type, TAC.Machine vm) {
			return type == vm.functionType;
		}

		public override int Hash() {
			return function.GetHashCode();
		}

		public override double Equality(Value rhs) {
			// Two Function values are equal only if they refer to the exact same function
			if (!(rhs is ValFunction)) return 0;
			var other = (ValFunction)rhs;
			return function == other.function ? 1 : 0;
		}

        public ValFunction BindAndCopy(ValMap contextVariables) {
            return new ValFunction(function, contextVariables);
        }

	}

	public class ValTemp : Value {
		public int tempNum;

		public ValTemp(int tempNum) {
			this.tempNum = tempNum;
		}

		public override Value Val(TAC.Context context) {
			return context.GetTemp(tempNum);
		}

		public override Value Val(TAC.Context context, out ValMap valueFoundIn) {
			valueFoundIn = null;
			return context.GetTemp(tempNum);
		}

		public override string ToString(TAC.Machine vm) {
			return "_" + tempNum.ToString(CultureInfo.InvariantCulture);
		}

		public override int Hash() {
			return tempNum.GetHashCode();
		}

		public override double Equality(Value rhs) {
			return rhs is ValTemp && ((ValTemp)rhs).tempNum == tempNum ? 1 : 0;
		}

	}

	public class ValVar : Value {
		public enum LocalOnlyMode { Off, Warn, Strict };
		
		public string identifier;
		public bool noInvoke;	// reflects use of "@" (address-of) operator
		public LocalOnlyMode localOnly = LocalOnlyMode.Off;	// whether to look this up in the local scope only
		
		public ValVar(string identifier) {
			this.identifier = identifier;
		}

		public override Value Val(TAC.Context context) {
			if (this == self) return context.self;
			return context.GetVar(identifier);
		}

		public override Value Val(TAC.Context context, out ValMap valueFoundIn) {
			valueFoundIn = null;
			if (this == self) return context.self;
			return context.GetVar(identifier, localOnly);
		}

		public override string ToString(TAC.Machine vm) {
			if (noInvoke) return "@" + identifier;
			return identifier;
		}

		public override int Hash() {
			return identifier.GetHashCode();
		}

		public override double Equality(Value rhs) {
			return rhs is ValVar && ((ValVar)rhs).identifier == identifier ? 1 : 0;
		}

		// Special name for the implicit result variable we assign to on expression statements:
		public static ValVar implicitResult = new ValVar("_");

		// Special var for 'self'
		public static ValVar self = new ValVar("self");
	}

	public class ValSeqElem : Value {
		public Value sequence;
		public Value index;
		public bool noInvoke;	// reflects use of "@" (address-of) operator

		public ValSeqElem(Value sequence, Value index) {
			this.sequence = sequence;
			this.index = index;
		}

		/// <summary>
		/// Look up the given identifier in the given sequence, walking the type chain
		/// until we either find it, or fail.
		/// </summary>
		/// <param name="sequence">Sequence (object) to look in.</param>
		/// <param name="identifier">Identifier to look for.</param>
		/// <param name="context">Context.</param>
		public static Value Resolve(Value sequence, string identifier, TAC.Context context, out ValMap valueFoundIn) {
			var includeMapType = true;
			valueFoundIn = null;
			int loopsLeft = ValMap.maxIsaDepth;
			while (sequence != null) {
				if (sequence is ValTemp || sequence is ValVar) sequence = sequence.Val(context);
				if (sequence is ValMap) {
					// If the map contains this identifier, return its value.
					Value result = null;
					var idVal = TempValString.Get(identifier);
					bool found = ((ValMap)sequence).map.TryGetValue(idVal, out result);
					TempValString.Release(idVal);
					if (found) {
						valueFoundIn = (ValMap)sequence;
						return result;
					}
					
					// Otherwise, if we have an __isa, try that next.
					if (loopsLeft < 0) throw new LimitExceededException("__isa depth exceeded (perhaps a reference loop?)"); 
					if (!((ValMap)sequence).map.TryGetValue(ValString.magicIsA, out sequence)) {
						// ...and if we don't have an __isa, try the generic map type if allowed
						if (!includeMapType) throw new KeyException(identifier);
						sequence = context.vm.mapType ?? Intrinsics.MapType();
						includeMapType = false;
					}
				} else if (sequence is ValList) {
					sequence = context.vm.listType ?? Intrinsics.ListType();
					includeMapType = false;
				} else if (sequence is ValString) {
					sequence = context.vm.stringType ?? Intrinsics.StringType();
					includeMapType = false;
				} else if (sequence is ValNumber) {
					sequence = context.vm.numberType ?? Intrinsics.NumberType();
					includeMapType = false;
				} else if (sequence is ValFunction) {
					sequence = context.vm.functionType ?? Intrinsics.FunctionType();
					includeMapType = false;
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
					throw new TypeException("Type Error (while attempting to look up " + identifier + ")");
				}
				loopsLeft--;
			}
			return null;
		}

		public override Value Val(TAC.Context context) {
			ValMap ignored;
			return Val(context, out ignored);
		}
		
		public override Value Val(TAC.Context context, out ValMap valueFoundIn) {
			Value baseSeq = sequence;
			if (sequence == ValVar.self) {
				baseSeq = context.self;
				if (baseSeq == null) throw new UndefinedIdentifierException("self");
			}
			valueFoundIn = null;
			Value idxVal = index == null ? null : index.Val(context);
			if (idxVal is ValString) return Resolve(baseSeq, ((ValString)idxVal).value, context, out valueFoundIn);
			// Ok, we're searching for something that's not a string;
			// this can only be done in maps, lists, and strings (and lists/strings, only with a numeric index).
			Value baseVal = baseSeq.Val(context);
			if (baseVal is ValMap) {
				Value result = ((ValMap)baseVal).Lookup(idxVal, out valueFoundIn);
				if (valueFoundIn == null) throw new KeyException(idxVal.CodeForm(context.vm, 1));
				return result;
			} else if (baseVal is ValList) {
				return ((ValList)baseVal).GetElem(idxVal);
			} else if (baseVal is ValString) {
				return ((ValString)baseVal).GetElem(idxVal);
			} else if (baseVal is null) {
				throw new TypeException("Null Reference Exception: can't index into null");
			}
				
			throw new TypeException("Type Exception: can't index into this type");
		}

		public override string ToString(TAC.Machine vm) {
			return string.Format("{0}{1}[{2}]", noInvoke ? "@" : "", sequence, index);
		}

		public override int Hash() {
			return sequence.Hash() ^ index.Hash();
		}

		public override double Equality(Value rhs) {
			return rhs is ValSeqElem && ((ValSeqElem)rhs).sequence == sequence
				&& ((ValSeqElem)rhs).index == index ? 1 : 0;
		}

	}

	public class RValueEqualityComparer : IEqualityComparer<Value> {
		public bool Equals(Value val1, Value val2) {
			return val1.Equality(val2) > 0;
		}

		public int GetHashCode(Value val) {
			return val.Hash();
		}

		static RValueEqualityComparer _instance = null;
		public static RValueEqualityComparer instance {
			get {
				if (_instance == null) _instance = new RValueEqualityComparer();
				return _instance;
			}
		}
	}
	
}

