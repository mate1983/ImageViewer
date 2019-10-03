#setting title, Gamma Correction
#setting description, Nonlinear operation used to encode and decode luminance or tristimulus values in video or still image systems. Formula: |Factor * V| ^ (1/Gamma).

#param Gamma, gamma, float, 1, 0
#paramprop Gamma, onSubtract, -0.1, add
#paramprop Gamma, onAdd, 0.1, add

#param Factor, factor, float, 1.0, 0

#param Per Channel Gamma, perChannel, bool, false
#param Keep Sign, keepSign, bool, true

#keybinding Factor, Add, 2.0, multiply
#keybinding Factor, OemPlus, 2.0, multiply
#keybinding Factor, Subtract, 0.5, multiply
#keybinding Factor, OemMinus, 0.5, multiply

float4 filter(uint2 pixelCoord, uint2 size)
{
	float4 color = src_image[pixelCoord];
	
	float3 sgn = sign(color.rgb);
	color.rgb = abs(color.rgb * factor);
	if(perChannel)
	{
		color.rgb = pow(color.rgb, float3(1.0 / gamma));
	}
	else
	{
		// this luminance looks better
		float lum = dot(color.rgb, vec3(0.299, 0.587, 0.114));
		//float lum = dot(color.rgb, vec3(0.2126, 0.7152, 0.0722));
		if(lum > 0)
		{
			float newLum = pow(lum, 1.0/gamma);
			color.rgb = color.rgb / lum * newLum;
		}
	}
	if(keepSign)
		color.rgb *= sgn;
	
	return color;
}