void CalculateClipFactor_float(float maskIn, float idIn, out float visibilityOut) 
{
	
	int maskInt = (int)maskIn;
	int idInt = (int)idIn;

	visibilityOut = (float)((maskInt & idInt) == 0 ? (idInt <= 0 ? 1 : 0) : 1);

}