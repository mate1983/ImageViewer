#setting title, Luminance values
#setting description, Transforms the image into a grayscale image with luminance values. color = dot((r,g,b),(0.2126, 0.7152, 0.0722)). Alpha value remains unchanged.
#setting type, COLOR

#param Use float3(0.299 0.587 0.114) instead, useOtherLuminance, bool, false

float4 filter(float4 color)
{
	float3 lum = float3(0.2126, 0.7152, 0.0722);
	if(useOtherLuminance) lum = float3(0.299, 0.587, 0.114);
	float val = dot(color.rgb, lum);
	return float4(float3(val, val, val), color.a);
}