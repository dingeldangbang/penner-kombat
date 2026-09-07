shader_type spatial;
render_mode blend_mix;

// Nass-Asphalt-Arena-Boden mit prozeduralen Pfützen, Spiegelglanz (SSR-freundlich)
// und dezenten Neon-Rändern – Referenz: MKX-Teich/Industrieböden.

uniform vec4 base_color : source_color = vec4(0.045, 0.05, 0.075, 1.0);
uniform vec4 puddle_color : source_color = vec4(0.02, 0.03, 0.055, 1.0);
uniform vec4 accent_color : source_color = vec4(1.0, 0.16, 0.12, 1.0);
uniform float wetness : hint_range(0.0, 1.0) = 0.85;
uniform float noise_scale : hint_range(4.0, 64.0) = 14.0;
uniform float grid_glow : hint_range(0.0, 2.0) = 0.45;
uniform float reflection_strength : hint_range(0.0, 1.0) = 0.7;
uniform float ripple_strength : hint_range(0.0, 1.0) = 0.35;

varying vec2 v_uv;
varying vec3 v_world;

void vertex() {
	v_uv = UV;
	v_world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123);
}

void fragment() {
	// Ruhige, langsame "Wellen" für den nassen Fugenbeton
	float wave = sin(v_uv.x * noise_scale * 1.7 + sin(v_uv.y * noise_scale * 0.9 + TIME * 0.35) * 1.4) * 0.5 + 0.5;
	float block = hash(floor(v_uv * vec2(noise_scale * 0.75, noise_scale * 0.5)));
	float puddle_mask = smoothstep(0.42, 0.82, wave * 0.65 + block * 0.35);

	float wet = mix(0.35, 1.0, puddle_mask) * wetness;

	vec3 albedo = mix(base_color.rgb, puddle_color.rgb, puddle_mask);
	ALBEDO = albedo;

	// Pfützen = nass & dunkel, Rest matter
	ROUGHNESS = mix(0.85, 0.045, wet);
	METALLIC = 0.0;
	SPECULAR = mix(0.3, 0.95, wet);

	// Fresnel-Spiegelung am Bodenrand (wirkt mit SSR ultra-stark, tut aber auch ohne)
	vec3 view_dir = normalize(CAMERA_POSITION_WORLD - v_world);
	float fresnel = pow(1.0 - abs(dot(normalize(NORMAL), view_dir)), 3.0);
	EMISSION += accent_color.rgb * fresnel * reflection_strength * 0.6;

	// Neon-Gitterlinien auf dem Boden (MKX-Stage-Branding)
	vec2 grid = abs(fract(v_uv * vec2(noise_scale * 2.0)) - 0.5);
	float line = 1.0 - smoothstep(0.485, 0.5, max(grid.x, grid.y));
	EMISSION += accent_color.rgb * line * grid_glow * (0.7 + 0.3 * sin(TIME * 2.0));

	// feine Regenspritzer ("Sparkle")
	float sparkle = step(0.9985, hash(floor(v_uv * noise_scale * 6.0) + floor(TIME * 9.0))) * wet * ripple_strength;
	EMISSION += vec3(0.7, 0.75, 0.9) * sparkle;
}
