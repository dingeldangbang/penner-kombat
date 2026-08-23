shader_type spatial;
render_mode blend_mix;

uniform float intensity : hint_range(0.0, 2.0) = 0.0;
uniform vec4 color : source_color = vec4(0.9, 0.7, 0.2, 1.0);

void fragment() {
	float shine = pow(1.0 - abs(dot(normalize(NORMAL), normalize(VIEW))), 3.0);
	ALBEDO = mix(vec3(0.65, 0.52, 0.32), color.rgb, clamp(intensity * 0.45, 0.0, 1.0));
	ROUGHNESS = mix(0.75, 0.08, clamp(intensity, 0.0, 1.0));
	SPECULAR = mix(0.35, 1.0, clamp(intensity, 0.0, 1.0));
	EMISSION = color.rgb * shine * intensity * 0.35;
}
