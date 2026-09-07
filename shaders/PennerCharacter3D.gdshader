shader_type spatial;
render_mode blend_mix, specular_schlick_ggx, diffuse_burley;

// Penner-Kombat 3D-Charakter-Shader (MKX-inspirierter "Toon-PBR"):
//  - PBR-Basis mit Albedo-Textur/Tönung aus glb/StandardMaterial
//  - weiche Zell-/Toon-Bänderung (Ken Burns statt harter Flächen)
//  - Rim-Light (Fresnel) in Accentfarbe, wie im MKX-Neon-Look
//  - billige Silhouetten-Outline über Fresnel-Verdunkelung
//  - leichte "wet skin" Spekularität für brutale Treffer
//
// Wird zur Laufzeit von CharacterShaderBinder auf geriggte Modelle angewendet.

uniform vec4 albedo_tint : source_color = vec4(1.0);
uniform sampler2D albedo_texture : source_color, filter_linear_mipmap;
uniform float roughness : hint_range(0.0, 1.0) = 0.45;
uniform float metallic : hint_range(0.0, 1.0) = 0.0;
uniform float rim_strength : hint_range(0.0, 3.0) = 0.9;
uniform vec4 rim_color : source_color = vec4(1.0, 0.62, 0.2, 1.0);
uniform float toon_bands : hint_range(1.0, 8.0) = 2.0;
uniform float outline_strength : hint_range(0.0, 1.0) = 0.3;
uniform vec4 outline_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform float emission_energy : hint_range(0.0, 8.0) = 0.0;
uniform vec3 emission_color : source_color = vec3(1.0);
uniform float hit_flash : hint_range(0.0, 1.0) = 0.0;
uniform vec4 hit_color : source_color = vec4(1.0, 0.85, 0.4, 1.0);

varying vec3 v_world;
varying vec3 v_normal;
varying vec2 v_uv;

void vertex() {
	v_world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	v_normal = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);
	v_uv = UV;
}

void fragment() {
	vec3 n = normalize(v_normal);
	vec3 v = normalize(CAMERA_POSITION_WORLD - v_world);
	float ndv = max(dot(n, v), 0.0);

	vec4 base = albedo_tint * texture(albedo_texture, v_uv);

	// Silhouetten-Outline: Kanten (geringe Rückwärts-Reflexion) abdunkeln
	float outline = pow(1.0 - abs(dot(n, v)), 3.0) * outline_strength;
	vec3 albedo = mix(base.rgb, outline_color.rgb, clamp(outline, 0.0, 0.9));

	ALBEDO = albedo;
	ROUGHNESS = roughness;
	METALLIC = metallic;
	SPECULAR = 0.6;

	// Rim-Light: Neon-Streifen am Modellrand
	RIM = rim_color.rgb * rim_strength * pow(1.0 - ndv, 2.2);
	RIM_MASK = pow(1.0 - ndv, 1.6);

	// Treffer-Blitz: kurzes Aufleuchten bei Impact
	EMISSION = emission_color * emission_energy;
	EMISSION += hit_color.rgb * hit_flash * 2.0;
	ALBEDO = mix(ALBEDO, hit_color.rgb * 0.9, clamp(hit_flash, 0.0, 1.0));
}

// Weiche Zellschattierung statt harter Diffuse
void light() {
	float ndl = max(dot(NORMAL, LIGHT), 0.0);
	float band = floor(ndl * toon_bands) / toon_bands;
	DIFFUSE_LIGHT += ATTENUATION * band;

	vec3 half_vec = normalize(LIGHT + VIEW);
	float spec = pow(max(dot(NORMAL, half_vec), 0.0), 32.0);
	SPECULAR_LIGHT += ATTENUATION * spec * 0.5;
}
