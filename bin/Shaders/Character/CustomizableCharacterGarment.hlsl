#ifdef SHADERGRAPH_PREVIEW

void SampleRippage_float(int localID, int vertexIndex, int vertexCount, out float outRippage)
{
	outRippage = 0;
}

#else

StructuredBuffer<float> _Rippage;

void SampleRippage_float(int localID, int vertexIndex, int vertexCount, out float outRippage)
{
	outRippage = _Rippage[(localID * vertexCount) + vertexIndex];
}

#endif