shader_type spatial;
render_mode blend_mix;

uniform float pulse_intensity : hint_range(0.0, 1.5) = 0.0;
uniform vec4 pulse_color : source_color = vec4(0.2, 0.8, 0.2, 1.0);
uniform bool is_warning = false;

void fragment() {
	float pulse = sin(TIME * (8.0 + pulse_intensity * 16.0)) * 0.5 + 0.5;
	float fresnel = pow(1.0 - abs(dot(normalize(NORMAL), normalize(VIEW))), 2.0);
	vec3 warning = is_warning ? vec3(1.0, 0.0, 0.0) : pulse_color.rgb;
	ALBEDO = mix(vec3(0.35, 0.35, 0.38), warning, pulse_intensity * 0.25);
	EMISSION = warning * (pulse * 0.35 + fresnel) * pulse_intensity;
}
