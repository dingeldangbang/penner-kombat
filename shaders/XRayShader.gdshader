shader_type spatial;
render_mode blend_mix, unshaded, cull_disabled, depth_draw_always;

uniform float intensity : hint_range(0.0, 1.0) = 0.0;
uniform vec4 skeleton_color : source_color = vec4(0.8, 0.2, 0.2, 1.0);
uniform vec4 skin_color : source_color = vec4(0.9, 0.7, 0.5, 1.0);
global uniform vec4 xray_uniform;

varying vec3 world_pos;

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	float xray_intensity = clamp(max(intensity, xray_uniform.r), 0.0, 1.0);
	vec3 normal_dir = normalize(NORMAL);
	vec3 view_dir = normalize(CAMERA_POSITION_WORLD - world_pos);
	float fresnel = pow(1.0 - abs(dot(view_dir, normal_dir)), 2.0) * xray_intensity;
	vec4 color = mix(skin_color, skeleton_color, xray_intensity);
	color.rgb += fresnel * skeleton_color.rgb * 2.0;
	color.rgb += xray_intensity * xray_intensity * vec3(0.5, 0.2, 0.2);
	ALBEDO = color.rgb;
	ALPHA = max(color.a, xray_intensity * 0.65);
}
