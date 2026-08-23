shader_type spatial;
render_mode blend_add, unshaded, cull_disabled;

uniform float intensity : hint_range(0.0, 3.0) = 0.0;
uniform vec4 gold_color : source_color = vec4(1.0, 0.84, 0.0, 1.0);

void fragment() {
	float fresnel = pow(1.0 - abs(dot(normalize(NORMAL), normalize(VIEW))), 2.0);
	ALBEDO = gold_color.rgb;
	EMISSION = gold_color.rgb * intensity * (0.5 + fresnel * 2.0);
	ALPHA = clamp(intensity * (0.25 + fresnel), 0.0, 1.0);
}
