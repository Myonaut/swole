#ifdef SHADERGRAPH_PREVIEW

void SamplePerVertexMasking_float(int localID, int vertexIndex, int vertexCount, out float masking)
{
	masking = 0;
}

#else

StructuredBuffer<float> _PerVertexMasking;

void SamplePerVertexMasking_float(int localID, int vertexIndex, int vertexCount, out float masking)
{
	masking = _PerVertexMasking[(localID * vertexCount) + vertexIndex];
}

#endif