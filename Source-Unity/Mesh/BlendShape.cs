#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{

    [Serializable]
    public class BlendShape : ICloneable
    {

        public string name;

        /// <summary>
        /// Convenience field for storing a string
        /// </summary>
        public string tag;

        /// <summary>
        /// Convenience field for storing an index
        /// </summary>
        public int index;

        public Frame[] frames = new Frame[0];

        public void RemapFrameWeights(float finalTargetWeight)
        {
            if (frames == null || frames.Length <= 0) return;

            float originalMaxWeight = 0f;
            for (int a = 0; a < frames.Length; a++) if (frames[a].weight > originalMaxWeight) originalMaxWeight = frames[a].weight;
            if (originalMaxWeight == 0) return;

            for (int a = 0; a < frames.Length; a++) frames[a].weight = (frames[a].weight / originalMaxWeight) * finalTargetWeight; 
        }

        public BlendShape() { }
        public BlendShape(string name)
        {
            this.name = name;
        }

        public object Clone() => Duplicate();
        public BlendShape Duplicate()
        {
            BlendShape shape = new BlendShape(name);

            shape.frames = new Frame[frames.Length];
            for (int a = 0; a < frames.Length; a++)
            {
                Frame frame = frames[a].Duplicate();
                frame.shape = shape;
                shape.frames[a] = frame;
            }

            return shape;
        }

        public BlendShape AsRelative(BlendShape baseShape)
        {

            BlendShape deltaShape = new BlendShape(name);

            Frame prevFrame = null;

            if (baseShape.frames.Length > frames.Length)
            {

                for (int a = 0; a < baseShape.frames.Length; a++)
                {

                    Frame baseFrame = baseShape.frames[a];

                    Frame localFrame = null;

                    if (a < frames.Length) localFrame = frames[a];

                    if (prevFrame == null) prevFrame = localFrame;

                    deltaShape.AddFrame((localFrame == null ? prevFrame : localFrame).AsRelative(baseFrame));

                }

            }
            else
            {

                for (int a = 0; a < frames.Length; a++)
                {

                    Frame localFrame = frames[a];

                    Frame baseFrame = null;

                    if (a < baseShape.frames.Length) baseFrame = baseShape.frames[a];

                    if (prevFrame == null) prevFrame = localFrame;

                    deltaShape.AddFrame(baseFrame == null ? (prevFrame == null ? localFrame : prevFrame) : localFrame.AsRelative(baseFrame));

                }

            }

            return deltaShape;

        }

        public void MakeRelative(BlendShape baseShape)
        {

            Frame prevFrame = null;

            if (baseShape.frames.Length > frames.Length)
            {

                int origFramesLength = frames.Length;

                for (int a = 0; a < baseShape.frames.Length; a++)
                {

                    Frame baseFrame = baseShape.frames[a];

                    Frame localFrame = null;

                    if (a < origFramesLength) localFrame = frames[a];

                    if (prevFrame == null) prevFrame = localFrame;

                    var newFrame = (localFrame == null ? prevFrame : localFrame).AsRelative(baseFrame);

                    if (a >= origFramesLength)
                    {

                        AddFrame(newFrame);

                    }
                    else
                    {

                        frames[a] = newFrame;

                    }

                }

            }
            else
            {

                for (int a = 0; a < frames.Length; a++)
                {

                    Frame localFrame = frames[a];

                    Frame baseFrame = null;

                    if (a < baseShape.frames.Length) baseFrame = baseShape.frames[a];

                    if (prevFrame == null) prevFrame = localFrame;

                    frames[a] = baseFrame == null ? (prevFrame == null ? localFrame : prevFrame) : localFrame.AsRelative(baseFrame);

                }

            }

        }

        public BlendShape(Mesh mesh, string name, bool expandable = false)
        {

            this.name = name;

            int id = mesh.GetBlendShapeIndex(name);

            if (id < 0) return;

            int count = mesh.GetBlendShapeFrameCount(id);

            for (int a = 0; a < count; a++)
            {

                float weight = mesh.GetBlendShapeFrameWeight(id, a);

                Vector3[] dV = new Vector3[mesh.vertexCount];
                Vector3[] dN = new Vector3[mesh.vertexCount];
                Vector3[] dT = new Vector3[mesh.vertexCount];

                mesh.GetBlendShapeFrameVertices(id, a, dV, dN, dT);

                AddFrame(weight, dV, dN, dT, expandable);

            }

        }

        public BlendShape(string name, int vertexCount, bool expandable = false) : this(name, null, vertexCount, expandable) { }
        public BlendShape(string name, Frame[] referenceFrames, int vertexCount, bool expandable = false)
        {

            this.name = name;

            if (referenceFrames == null || referenceFrames.Length <= 0)
            {
                float weight = 1;

                Vector3[] dV = new Vector3[vertexCount];
                Vector3[] dN = new Vector3[vertexCount];
                Vector3[] dT = new Vector3[vertexCount];

                AddFrame(weight, dV, dN, dT, expandable);
            }
            else
            {
                for (int a = 0; a < referenceFrames.Length; a++)
                {

                    float weight = referenceFrames[a].weight;

                    Vector3[] dV = new Vector3[vertexCount];
                    Vector3[] dN = new Vector3[vertexCount];
                    Vector3[] dT = new Vector3[vertexCount];

                    AddFrame(weight, dV, dN, dT, expandable);

                }
            }
        }

        public int AddToMesh(Mesh mesh)
        {

            for (int a = 0; a < frames.Length; a++)
            {

                BlendShape.Frame frame = frames[a];

                frame.Expand();

                mesh.AddBlendShapeFrame(name, frame.weight, frame.deltaVertices.ToArray(), frame.deltaNormals.ToArray(), frame.deltaTangents.ToArray());

            }

            return mesh.GetBlendShapeIndex(name);

        }

        public void Expand()
        {

            for (int a = 0; a < frames.Length; a++) frames[a].Expand();

        }

        public void Reset()
        {

            for (int a = 0; a < frames.Length; a++) frames[a].Reset();

        }

        public void Unexpand()
        {

            for (int a = 0; a < frames.Length; a++) frames[a].Unexpand();

        }

        public Frame AddFrame(float weight, Vector3[] deltaVertices, Vector3[] deltaNormals, Vector3[] deltaTangents)
        {

            return AddFrame(weight, deltaVertices, deltaNormals, deltaTangents, false);

        }

        public Frame AddFrame(float weight, Vector3[] deltaVertices, Vector3[] deltaNormals, Vector3[] deltaTangents, bool expandable)
        {

            Frame frame = new Frame(this, frames.Length, weight, deltaVertices, deltaNormals, deltaTangents, expandable);

            Frame[] na = new Frame[frame.index + 1];

            for (int a = 0; a < frame.index; a++) na[a] = frames[a];

            na[frame.index] = frame;

            frames = na;

            return frame;

        }

        public Frame AddFrame(float weight, Vector3[] deltaVertices, Vector3[] deltaNormals, Vector4[] deltaTangents)
        {
            Vector3[] deltaTangents_ = new Vector3[deltaTangents.Length];
            for (int a = 0; a < deltaTangents.Length; a++) deltaTangents_[a] = deltaTangents[a];
            return AddFrame(weight, deltaVertices, deltaNormals, deltaTangents_, false);

        }

        public Frame AddFrame(float weight, Vector3[] deltaVertices, Vector3[] deltaNormals, Vector4[] deltaTangents, bool expandable)
        {
            Vector3[] deltaTangents_ = new Vector3[deltaTangents.Length];
            for (int a = 0; a < deltaTangents.Length; a++) deltaTangents_[a] = deltaTangents[a];
            return AddFrame(weight, deltaVertices, deltaNormals, deltaTangents_, expandable);
        }

        public Frame AddFrame(Frame frame)
        {

            Frame[] na = new Frame[frames.Length + 1];

            int pos = frames.Length;

            for (int a = 0; a < frames.Length; a++)
            {

                if (frames[a].weight > frame.weight)
                {

                    pos = a;

                    break;

                }

            }

            for (int a = pos; a < frames.Length; a++)
            {

                na[a + 1] = frames[a];

            }

            na[pos] = frame;

            frames = na;

            return frame;

        }

        public Frame AddFrame(float weight, Frame frame)
        {
            frame = frame.Duplicate();
            frame.weight = weight;

            return AddFrame(frame);
        }

        public Vector3[][] GetDeltaVertices()
        {

            Vector3[][] deltaVertices = new Vector3[frames.Length][];

            for (int a = 0; a < deltaVertices.Length; a++) deltaVertices[a] = frames[a].deltaVertices.ToArray();

            return deltaVertices;

        }

        public Vector3[][] GetDeltaNormals()
        {

            Vector3[][] deltaNormals = new Vector3[frames.Length][];

            for (int a = 0; a < deltaNormals.Length; a++) deltaNormals[a] = frames[a].deltaNormals.ToArray();

            return deltaNormals;

        }

        public Vector3[][] GetDeltaTangents()
        {

            Vector3[][] deltaTangents = new Vector3[frames.Length][];

            for (int a = 0; a < deltaTangents.Length; a++) deltaTangents[a] = frames[a].deltaTangents.ToArray();

            return deltaTangents;

        }

        [Serializable]
        public abstract class FrameData : ICloneable
        {
            public abstract int GetLength();
            public abstract void SetLength(int length);
            public int Length
            {
                get
                {
                    return GetLength();
                }

                set
                {
                    SetLength(value);
                }
            }

            public abstract Vector3 GetDelta(int i);
            public abstract void SetDelta(int i, Vector3 delta);
            public Vector3 this[int i]
            {
                get => GetDelta(i);
                set => SetDelta(i, value);
            }

            public abstract Vector3[] ToArray();
            public abstract void SetData(Vector3[] array);

            public object Clone() => Duplicate();
            public abstract FrameData Duplicate();

            public abstract FrameData AsRelative(FrameData baseData);
        }

        [Serializable]
        public class FrameDataLinear : FrameData
        {

            private Vector3[] internalArray;

            public FrameDataLinear(Vector3[] array)
            {
                internalArray = array;
            }

            public FrameDataLinear(int length)
            {
                internalArray = new Vector3[length];
            }

            public override int GetLength() => internalArray.Length;
            public override void SetLength(int length)
            {
                throw new NotImplementedException();
            }

            public override Vector3 GetDelta(int i)
            {
                return internalArray[i];
            }

            public override void SetDelta(int i, Vector3 delta)
            {
                internalArray[i] = delta;
            }

            public override Vector3[] ToArray()
            {
                return (Vector3[])internalArray.Clone();
            }
            public override void SetData(Vector3[] array)
            {
                internalArray = array;
            }

            public override FrameData Duplicate()
            {
                return new FrameDataLinear(ToArray());
            }

            public override FrameData AsRelative(FrameData baseData)
            {

                FrameDataLinear deltaData = new FrameDataLinear(new Vector3[internalArray.Length]);

                int baseLength = baseData.Length;
                for (int a = 0; a < GetLength(); a++)
                {
                    deltaData.SetDelta(a, GetDelta(a) - (a >= baseLength ? Vector3.zero : baseData.GetDelta(a)));
                }

                return deltaData;

            }

        }

        [Serializable]
        public class FrameDataMapped : FrameData
        {

            private float threshold = 0.00001f;

            private Dictionary<int, Vector3> internalData = new Dictionary<int, Vector3>();

            public void Clear() { internalData.Clear(); }

            public int vertexCount;

            public FrameDataMapped(Vector3[] array, float threshold = 0.00001f)
            {

                this.threshold = threshold;

                vertexCount = array.Length;

                internalData = new Dictionary<int, Vector3>();

                for (int a = 0; a < vertexCount; a++)
                {

                    Vector3 v = array[a];

                    if (Mathf.Abs(v.x) <= threshold && Mathf.Abs(v.y) <= threshold && Mathf.Abs(v.z) <= threshold) continue;

                    internalData[a] = v;

                }

            }

            public FrameDataMapped(int vertexCount)
            {

                this.vertexCount = vertexCount;

                internalData = new Dictionary<int, Vector3>();

            }

            public override int GetLength() => vertexCount;
            public override void SetLength(int length) => vertexCount = length;


            public override Vector3 GetDelta(int i)
            {

                if (internalData.TryGetValue(i, out Vector3 v)) return v;

                return Vector3.zero;

            }

            public override void SetDelta(int i, Vector3 delta)
            {

                if (Mathf.Abs(delta.x) <= threshold && Mathf.Abs(delta.y) <= threshold && Mathf.Abs(delta.z) <= threshold)
                {

                    internalData.Remove(i);

                    return;

                }

                internalData[i] = delta;

            }

            public override Vector3[] ToArray()
            {

                Vector3[] array = new Vector3[vertexCount];

                foreach (var pair in internalData) array[pair.Key] = pair.Value;

                return array;

            }

            public override void SetData(Vector3[] array)
            {
                internalData.Clear();
                if (array != null)
                {
                    for (int a = 0; a < array.Length; a++)
                    {
                        var data = array[a];
                        if (data.x == 0 && data.y == 0 && data.z == 0) continue;

                        internalData[a] = data;
                    }
                }
            }

            public override FrameData Duplicate()
            {

                FrameDataMapped data = new FrameDataMapped(vertexCount);

                foreach (var pair in internalData) data[pair.Key] = pair.Value;

                return data;

            }

            public override FrameData AsRelative(FrameData baseData)
            {

                FrameDataMapped deltaData = new FrameDataMapped(vertexCount);

                int baseLength = baseData.Length;

                foreach (var set in internalData)
                {

                    int key = set.Key;

                    deltaData.SetDelta(key, set.Value - (key >= baseLength ? Vector3.zero : baseData.GetDelta(key)));

                }

                return deltaData;

            }

        }

        [Serializable]
        public class Frame : ICloneable
        {

            public Frame(float weight, bool expandable = false)
            {

                this.weight = weight;

                if (expandable)
                {

                    expandable_deltaVertices = new List<Vector3>(deltaVertices.ToArray());
                    expandable_deltaNormals = new List<Vector3>(deltaNormals.ToArray());
                    expandable_deltaTangents = new List<Vector3>(deltaTangents.ToArray());

                }

            }

            public Frame(BlendShape shape, int index, float weight, Vector3[] deltaVertices, Vector3[] deltaNormals, Vector3[] deltaTangents, bool expandable)
            {
                this.shape = shape;
                this.index = index;
                this.weight = weight;
                this.deltaVertices = new FrameDataLinear(deltaVertices);
                this.deltaNormals = new FrameDataLinear(deltaNormals);
                this.deltaTangents = new FrameDataLinear(deltaTangents);

                if (expandable)
                {

                    expandable_deltaVertices = new List<Vector3>(deltaVertices);
                    expandable_deltaNormals = new List<Vector3>(deltaNormals);
                    expandable_deltaTangents = new List<Vector3>(deltaTangents);

                }

            }

            public Frame(BlendShape shape, int index, float weight, FrameData deltaVertices, FrameData deltaNormals, FrameData deltaTangents, bool expandable)
            {
                this.shape = shape;
                this.index = index;
                this.weight = weight;
                this.deltaVertices = deltaVertices;
                this.deltaNormals = deltaNormals;
                this.deltaTangents = deltaTangents;

                if (expandable)
                {

                    expandable_deltaVertices = new List<Vector3>(deltaVertices.ToArray());
                    expandable_deltaNormals = new List<Vector3>(deltaNormals.ToArray());
                    expandable_deltaTangents = new List<Vector3>(deltaTangents.ToArray());

                }

            }

            public Frame(BlendShape shape, int index, float weight, Vector3[] deltaVertices, Vector3[] deltaNormals, Vector3[] deltaTangents) : this(shape, index, weight, deltaVertices, deltaNormals, deltaTangents, false)
            { }

            public float weight;

            public int index;

            [NonSerialized]
            public BlendShape shape;

            [HideInInspector]
            public FrameData deltaVertices;

            [HideInInspector]
            public FrameData deltaNormals;

            [HideInInspector]
            public FrameData deltaTangents;

            [HideInInspector, NonSerialized]
            public List<Vector3> expandable_deltaVertices;
            [HideInInspector, NonSerialized]
            public List<Vector3> expandable_deltaNormals;
            [HideInInspector, NonSerialized]
            public List<Vector3> expandable_deltaTangents;

            public void Expand()
            {

                if (expandable_deltaVertices != null && expandable_deltaVertices.Count > 0) deltaVertices = new FrameDataLinear(expandable_deltaVertices.ToArray());
                if (expandable_deltaNormals != null && expandable_deltaNormals.Count > 0) deltaNormals = new FrameDataLinear(expandable_deltaNormals.ToArray());
                if (expandable_deltaTangents != null && expandable_deltaTangents.Count > 0) deltaTangents = new FrameDataLinear(expandable_deltaTangents.ToArray());

            }

            public void Reset()
            {

                expandable_deltaVertices = new List<Vector3>(deltaVertices.ToArray());
                expandable_deltaNormals = new List<Vector3>(deltaNormals.ToArray());
                expandable_deltaTangents = new List<Vector3>(deltaTangents.ToArray());

            }

            public void Unexpand()
            {

                expandable_deltaNormals = null;
                expandable_deltaTangents = null;
                expandable_deltaVertices = null;

            }

            public void Clear()
            {

                deltaVertices = new FrameDataLinear(0);
                deltaNormals = new FrameDataLinear(0);
                deltaTangents = new FrameDataLinear(0);

                if (expandable_deltaNormals != null) Reset(); else Unexpand();

            }

            public object Clone() => Duplicate();
            public Frame Duplicate()
            {

                Frame frame = new Frame(shape, index, weight, deltaVertices.Duplicate(), deltaNormals.Duplicate(), deltaTangents.Duplicate(), false);

                if (expandable_deltaNormals != null && expandable_deltaNormals.Count > 0)
                {
                    frame.expandable_deltaNormals = new List<Vector3>();
                    frame.expandable_deltaNormals.AddRange(expandable_deltaNormals);
                }

                if (expandable_deltaVertices != null && expandable_deltaVertices.Count > 0)
                {
                    frame.expandable_deltaVertices = new List<Vector3>();
                    frame.expandable_deltaVertices.AddRange(expandable_deltaVertices);
                }

                if (expandable_deltaTangents != null && expandable_deltaTangents.Count > 0)
                {
                    frame.expandable_deltaTangents = new List<Vector3>();
                    frame.expandable_deltaTangents.AddRange(expandable_deltaTangents);
                }

                return frame;

            }

            public Frame AsRelative(Frame baseFrame)
            {

                Frame deltaFrame = new Frame(weight, false);

                deltaFrame.deltaVertices = deltaVertices.AsRelative(baseFrame.deltaVertices);
                deltaFrame.deltaNormals = deltaNormals.AsRelative(baseFrame.deltaNormals);
                deltaFrame.deltaTangents = deltaTangents.AsRelative(baseFrame.deltaTangents);

                return deltaFrame;

            }

            public void MakeRelative(Frame baseFrame)
            {

                deltaVertices = deltaVertices.AsRelative(baseFrame.deltaVertices);
                deltaNormals = deltaNormals.AsRelative(baseFrame.deltaNormals);
                deltaTangents = deltaTangents.AsRelative(baseFrame.deltaTangents);

            }

            public Vector3[] ApplyVertices(Vector3[] vertices, float weight)
            {

                for (int a = 0; a < Mathf.Min(vertices.Length, deltaVertices.Length); a++)
                {

                    vertices[a] = vertices[a] + deltaVertices[a] * weight;

                }

                return vertices;

            }

            public Vector3[] ApplyNormals(Vector3[] normals, float weight)
            {

                for (int a = 0; a < Mathf.Min(normals.Length, deltaNormals.Length); a++)
                {

                    normals[a] = normals[a] + deltaNormals[a] * weight;

                }

                return normals;

            }

            public Vector4[] ApplyTangents(Vector4[] tangents, float weight)
            {

                for (int a = 0; a < Mathf.Min(tangents.Length, deltaTangents.Length); a++)
                {

                    Vector3 dt = deltaTangents[a];

                    tangents[a] = tangents[a] + new Vector4(dt.x * weight, dt.y * weight, dt.z * weight, 0);

                }

                return tangents;

            }

        }

        public Vector3 GetTransformedVertex(Vector3 vertex, int vertexIndex, float weight, float multiplier = 1)
        {

            int frameCount = frames.Length;

            if (frameCount == 1)
            {

                Frame frame = frames[0];

                float w = weight / frame.weight;

                vertex = vertex + frame.deltaVertices[vertexIndex] * w * multiplier;

            }
            else
            {

                for (int a = 0; a < frameCount; a++)
                {

                    Frame frame = frames[a];

                    if (weight < frame.weight)
                    {

                        float w = weight / frame.weight;

                        if (a == 0)
                        {

                            vertex = vertex + frame.deltaVertices[vertexIndex] * w * multiplier;

                        }

                    }
                    else
                    {

                        if (a == frameCount - 1)
                        {

                            float w = 1 + ((weight - frame.weight) / frame.weight);

                            vertex = vertex + frame.deltaVertices[vertexIndex] * w * multiplier;

                        }
                        else
                        {

                            Frame frame2 = frames[a + 1];

                            if (weight >= frame2.weight) continue;

                            float w = (weight - frame.weight) / (frame2.weight - frame.weight);

                            vertex = vertex + Vector3.LerpUnclamped(frame.deltaVertices[vertexIndex], frame2.deltaVertices[vertexIndex], w) * multiplier;

                        }

                    }

                }

            }

            return vertex;

        }

        public Vector3 GetTransformedNormal(Vector3 normal, int vertexIndex, float weight, float multiplier = 1)
        {

            int frameCount = frames.Length;

            if (frameCount == 1)
            {

                Frame frame = frames[0];

                float w = weight / frame.weight;

                normal = normal + frame.deltaNormals[vertexIndex] * w * multiplier;

            }
            else
            {

                for (int a = 0; a < frameCount; a++)
                {

                    Frame frame = frames[a];

                    if (weight < frame.weight)
                    {

                        float w = weight / frame.weight;

                        if (a == 0)
                        {

                            normal = normal + frame.deltaNormals[vertexIndex] * w * multiplier;

                        }

                    }
                    else
                    {

                        if (a == frameCount - 1)
                        {

                            float w = 1 + ((weight - frame.weight) / frame.weight);

                            normal = normal + frame.deltaNormals[vertexIndex] * w * multiplier;

                        }
                        else
                        {

                            Frame frame2 = frames[a + 1];

                            if (weight >= frame2.weight) continue;

                            float w = (weight - frame.weight) / (frame2.weight - frame.weight);

                            normal = normal + Vector3.Lerp(frame.deltaNormals[vertexIndex], frame2.deltaNormals[vertexIndex], w) * multiplier;

                        }

                    }

                }

            }

            return normal;

        }

        public Vector4 GetTransformedTangent(Vector4 tangent, int vertexIndex, float weight, float multiplier = 1)
        {

            int frameCount = frames.Length;

            if (frameCount == 1)
            {

                Frame frame = frames[0];

                float w = weight / frame.weight;

                Vector3 dt = frame.deltaTangents[vertexIndex];

                float wm = w * multiplier;

                tangent = tangent + new Vector4(dt.x * wm, dt.y * wm, dt.z * wm, 0);

            }
            else
            {

                for (int a = 0; a < frameCount; a++)
                {

                    Frame frame = frames[a];

                    if (weight < frame.weight)
                    {

                        float w = weight / frame.weight;

                        if (a == 0)
                        {

                            Vector3 dt = frame.deltaTangents[vertexIndex];

                            float wm = w * multiplier;

                            tangent = tangent + new Vector4(dt.x * wm, dt.y * wm, dt.z * wm, 0);

                        }

                    }
                    else
                    {

                        if (a == frameCount - 1)
                        {

                            float w = 1 + ((weight - frame.weight) / frame.weight);

                            Vector3 dt = frame.deltaTangents[vertexIndex];

                            float wm = w * multiplier;

                            tangent = tangent + new Vector4(dt.x * wm, dt.y * wm, dt.z * wm, 0);

                        }
                        else
                        {

                            Frame frame2 = frames[a + 1];

                            if (weight >= frame2.weight) continue;

                            float w = (weight - frame.weight) / (frame2.weight - frame.weight);

                            Vector3 dt = frame.deltaTangents[vertexIndex];
                            Vector3 dt2 = frame2.deltaTangents[vertexIndex];

                            tangent = tangent + Vector4.LerpUnclamped(new Vector4(dt.x, dt.y, dt.z, 0), new Vector4(dt2.x, dt2.y, dt2.z, 0), w) * multiplier;

                        }

                    }

                }

            }

            return tangent;

        }

        public Vector3[] GetTransformedVertices(Vector3[] originalVertices, float weight, bool createNewArray = true)
        {

            Vector3[] vertices = createNewArray ? (Vector3[])originalVertices.Clone() : originalVertices;

            int frameCount = frames.Length;

            int vertexCount = originalVertices.Length;

            if (frameCount == 1)
            {

                Frame frame = frames[0];

                float w = weight / frame.weight;

                for (int a = 0; a < vertexCount; a++) vertices[a] = vertices[a] + frame.deltaVertices[a] * w;

            }
            else
            {

                for (int a = 0; a < frameCount; a++)
                {

                    Frame frame = frames[a];

                    if (weight < frame.weight)
                    {

                        float w = weight / frame.weight;

                        if (a == 0)
                        {

                            for (int b = 0; b < vertexCount; b++) vertices[b] = vertices[b] + frame.deltaVertices[b] * w;

                        }

                    }
                    else
                    {

                        if (a == frameCount - 1)
                        {

                            float w = 1 + ((weight - frame.weight) / frame.weight);

                            for (int b = 0; b < vertexCount; b++) vertices[b] = vertices[b] + frame.deltaVertices[b] * w;

                        }
                        else
                        {

                            Frame frame2 = frames[a + 1];

                            if (weight >= frame2.weight) continue;

                            float w = (weight - frame.weight) / (frame2.weight - frame.weight);

                            for (int b = 0; b < vertexCount; b++) vertices[b] = vertices[b] + Vector3.LerpUnclamped(frame.deltaVertices[b], frame2.deltaVertices[b], w);

                        }

                    }

                }

            }

            return vertices;

        }

        public Vector3[] GetTransformedNormals(Vector3[] originalNormals, float weight, bool createNewArray = true)
        {

            Vector3[] normals = createNewArray ? (Vector3[])originalNormals.Clone() : originalNormals;

            int frameCount = frames.Length;

            int vertexCount = originalNormals.Length;

            if (frameCount == 1)
            {

                Frame frame = frames[0];

                float w = weight / frame.weight;

                for (int a = 0; a < vertexCount; a++) normals[a] = normals[a] + frame.deltaNormals[a] * w;

            }
            else
            {

                for (int a = 0; a < frameCount; a++)
                {

                    Frame frame = frames[a];

                    if (weight < frame.weight)
                    {

                        float w = weight / frame.weight;

                        if (a == 0)
                        {

                            for (int b = 0; b < vertexCount; b++) normals[b] = normals[b] + frame.deltaNormals[b] * w;

                        }

                    }
                    else
                    {

                        if (a == frameCount - 1)
                        {

                            float w = 1 + ((weight - frame.weight) / frame.weight);

                            for (int b = 0; b < vertexCount; b++) normals[b] = normals[b] + frame.deltaNormals[b] * w;

                        }
                        else
                        {

                            Frame frame2 = frames[a + 1];

                            if (weight >= frame2.weight) continue;

                            float w = (weight - frame.weight) / (frame2.weight - frame.weight);

                            for (int b = 0; b < vertexCount; b++) normals[b] = normals[b] + Vector3.LerpUnclamped(frame.deltaNormals[b], frame2.deltaNormals[b], w);

                        }

                    }

                }

            }

            return normals;

        }

        public Vector4[] GetTransformedTangents(Vector4[] originalTangents, float weight, bool createNewArray = true)
        {

            Vector4[] tangents = createNewArray ? (Vector4[])originalTangents.Clone() : originalTangents;

            int frameCount = frames.Length;

            int vertexCount = originalTangents.Length;

            if (frameCount == 1)
            {

                Frame frame = frames[0];

                float w = weight / frame.weight;

                for (int a = 0; a < vertexCount; a++)
                {

                    Vector3 dt = frame.deltaNormals[a];

                    tangents[a] = tangents[a] + new Vector4(dt.x, dt.y, dt.z, 0) * w;

                }

            }
            else
            {

                for (int a = 0; a < frameCount; a++)
                {

                    Frame frame = frames[a];

                    if (weight < frame.weight)
                    {

                        float w = weight / frame.weight;

                        if (a == 0)
                        {

                            for (int b = 0; b < vertexCount; b++)
                            {

                                Vector3 dt = frame.deltaNormals[a];

                                tangents[a] = tangents[a] + new Vector4(dt.x, dt.y, dt.z, 0) * w;

                            }

                        }

                    }
                    else
                    {

                        if (a == frameCount - 1)
                        {

                            float w = 1 + ((weight - frame.weight) / frame.weight);

                            for (int b = 0; b < vertexCount; b++)
                            {

                                Vector3 dt = frame.deltaNormals[a];

                                tangents[a] = tangents[a] + new Vector4(dt.x, dt.y, dt.z, 0) * w;

                            }

                        }
                        else
                        {

                            Frame frame2 = frames[a + 1];

                            if (weight >= frame2.weight) continue;

                            float w = (weight - frame.weight) / (frame2.weight - frame.weight);

                            for (int b = 0; b < vertexCount; b++)
                            {

                                Vector3 dt = frame.deltaNormals[b];
                                Vector3 dt2 = frame2.deltaNormals[b];

                                tangents[b] = tangents[b] + Vector4.LerpUnclamped(new Vector4(dt.x, dt.y, dt.z, 0), new Vector4(dt.x, dt.y, dt.z, 0), w);

                            }

                        }

                    }

                }

            }

            return tangents;

        }

        public void GetTransformedData(Vector3[] originalVertices, Vector3[] originalNormals, float weight, out Vector3[] vertices, out Vector3[] normals, bool createNewArrays = true)
        {

            vertices = createNewArrays ? (Vector3[])originalVertices.Clone() : originalVertices;

            normals = createNewArrays ? (Vector3[])originalNormals.Clone() : originalNormals;

            int frameCount = frames.Length;

            int vertexCount = originalVertices.Length;

            if (frameCount == 1)
            {

                Frame frame = frames[0];

                float w = weight / frame.weight;

                for (int a = 0; a < vertexCount; a++)
                {

                    vertices[a] = vertices[a] + frame.deltaVertices[a] * w;

                    normals[a] = normals[a] + frame.deltaNormals[a] * w;

                }

            }
            else
            {

                for (int a = 0; a < frameCount; a++)
                {

                    Frame frame = frames[a];

                    if (weight < frame.weight)
                    {

                        float w = weight / frame.weight;

                        if (a == 0)
                        {

                            for (int b = 0; b < vertexCount; b++)
                            {

                                vertices[b] = vertices[b] + frame.deltaVertices[b] * w;

                                normals[b] = normals[b] + frame.deltaNormals[b] * w;

                            }

                        }

                    }
                    else
                    {

                        if (a == frameCount - 1)
                        {

                            float w = 1 + ((weight - frame.weight) / frame.weight);

                            for (int b = 0; b < vertexCount; b++)
                            {

                                vertices[b] = vertices[b] + frame.deltaVertices[b] * w;

                                normals[b] = normals[b] + frame.deltaNormals[b] * w;

                            }

                        }
                        else
                        {

                            Frame frame2 = frames[a + 1];

                            if (weight >= frame2.weight) continue;

                            float w = (weight - frame.weight) / (frame2.weight - frame.weight);

                            for (int b = 0; b < vertexCount; b++)
                            {

                                vertices[b] = vertices[b] + Vector3.LerpUnclamped(frame.deltaVertices[b], frame2.deltaVertices[b], w);

                                normals[b] = normals[b] + Vector3.LerpUnclamped(frame.deltaNormals[b], frame2.deltaNormals[b], w);

                            }

                        }

                    }

                }

            }

        }

        public void GetTransformedData(Vector3[] originalVertices, Vector3[] originalNormals, Vector4[] originalTangents, float weight, out Vector3[] vertices, out Vector3[] normals, out Vector4[] tangents, bool createNewArrays = true)
        {

            vertices = createNewArrays ? (Vector3[])originalVertices.Clone() : originalVertices;

            normals = createNewArrays ? (Vector3[])originalNormals.Clone() : originalNormals;

            tangents = createNewArrays ? (Vector4[])originalTangents.Clone() : originalTangents;

            int frameCount = frames.Length;

            int vertexCount = originalVertices.Length;

            if (frameCount == 1)
            {

                Frame frame = frames[0];

                float w = weight / frame.weight;

                for (int a = 0; a < vertexCount; a++)
                {

                    vertices[a] = vertices[a] + frame.deltaVertices[a] * w;

                    normals[a] = normals[a] + frame.deltaNormals[a] * w;

                    tangents[a] = new Vector4(
                        tangents[a].x + frame.deltaTangents[a].x * w,
                        tangents[a].y + frame.deltaTangents[a].y * w,
                        tangents[a].z + frame.deltaTangents[a].z * w,
                        tangents[a].w);

                }

            }
            else
            {

                for (int a = 0; a < frameCount; a++)
                {

                    Frame frame = frames[a];

                    if (weight < frame.weight)
                    {

                        float w = weight / frame.weight;

                        if (a == 0)
                        {

                            for (int b = 0; b < vertexCount; b++)
                            {

                                vertices[b] = vertices[b] + frame.deltaVertices[b] * w;

                                normals[b] = normals[b] + frame.deltaNormals[b] * w;

                                tangents[b] = new Vector4(
                                    tangents[b].x + frame.deltaTangents[b].x * w,
                                    tangents[b].y + frame.deltaTangents[b].y * w,
                                    tangents[b].z + frame.deltaTangents[b].z * w,
                                    tangents[b].w);

                            }

                        }

                    }
                    else
                    {

                        if (a == frameCount - 1)
                        {

                            float w = 1 + ((weight - frame.weight) / frame.weight);

                            for (int b = 0; b < vertexCount; b++)
                            {

                                vertices[b] = vertices[b] + frame.deltaVertices[b] * w;

                                normals[b] = normals[b] + frame.deltaNormals[b] * w;

                                tangents[b] = new Vector4(
                                    tangents[b].x + frame.deltaTangents[b].x * w,
                                    tangents[b].y + frame.deltaTangents[b].y * w,
                                    tangents[b].z + frame.deltaTangents[b].z * w,
                                    tangents[b].w);

                            }

                        }
                        else
                        {

                            Frame frame2 = frames[a + 1];

                            if (weight >= frame2.weight) continue;

                            float w = (weight - frame.weight) / (frame2.weight - frame.weight);

                            for (int b = 0; b < vertexCount; b++)
                            {

                                vertices[b] = vertices[b] + Vector3.LerpUnclamped(frame.deltaVertices[b], frame2.deltaVertices[b], w);

                                normals[b] = normals[b] + Vector3.LerpUnclamped(frame.deltaNormals[b], frame2.deltaNormals[b], w);

                                Vector3 deltaTangent = Vector3.LerpUnclamped(frame.deltaTangents[b], frame2.deltaTangents[b], w);

                                tangents[b] = new Vector4(
                                    tangents[b].x + deltaTangent.x,
                                    tangents[b].y + deltaTangent.y,
                                    tangents[b].z + deltaTangent.z,
                                    tangents[b].w);

                            }

                        }

                    }

                }

            }

        }

    }

}
#endif