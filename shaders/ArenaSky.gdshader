shader_type spatial;
render_mode unshaded, cull_front, depth_draw_never, fog_disabled;

// MKX-artiger Nachthimmel: Verlauf, Horizont-Glühen, schwache Sterne.
uniform vec3 top_color : source_color = vec3(0.015, 0.018, 0.05);
uniform vec3 horizon_color : source_color = vec3(0.45, 0.08, 0.12);
uniform vec3 bottom_color : source_color = vec3(0.03, 0.03, 0.05);
uniform float horizon_glow : hint_range(0.0, 3.0) = 1.0;
uniform float star_density : hint_range(0.0, 1.0) = 0.35;
uniform float star_twinkle : hint_range(0.0, 1.0) = 0.25;

varying vec3 v_dir;

void vertex() {
	v_dir = normalize((MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz);
}

float hash13(vec3 p) {
	p = fract(p * 0.1031);
	p += dot(p, p.zyx + 31.32);
	return fract((p.x + p.y) * p.z);
}

void fragment() {
	vec3 dir = normalize(v_dir);
	float h = dir.y;

	vec3 col = mix(horizon_color, top_color, smoothstep(0.02, 0.6, h));
	col = mix(bottom_color, col, smoothstep(-0.35, 0.0, h));

	// Horizont-Glühen wie ein beleuchtetes Stadion hinter der Arena
	float glow = pow(1.0 - clamp(abs(h), 0.0, 1.0), 5.0);
	col += horizon_color * glow * horizon_glow;

	// Sterne (deterministisch pro Himmelszelle, leicht twinkelnd)
	if (star_density > 0.001 && h > 0.12) {
		vec3 cell = floor(dir * 180.0);
		float s = hash13(cell);
		float star = step(0.9965, s) * star_density;
		float tw = 0.75 + 0.25 * sin(TIME * (1.5 + s * 4.0) + s * 40.0) * star_twinkle;
		col = mix(col, vec3(1.0, 0.96, 0.9), clamp(star * tw, 0.0, 1.0));
	}

	ALBEDO = col;
}
