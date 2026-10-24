shader_type canvas_item;

uniform sampler2D screen_tex : hint_screen_texture, filter_linear_mipmap;
uniform float amount : hint_range(0.0, 1.0) = 0.0;
uniform float blur_lod : hint_range(0.0, 8.0) = 3.0;
uniform float dim : hint_range(0.0, 1.0) = 0.35;

void fragment() {
	vec2 texel = 1.0 / vec2(textureSize(screen_tex, 0));
	float lod = blur_lod * amount;
	vec2 radius = texel * exp2(lod);

	// Small kernel on top of a mip level to hide the blockiness of mip sampling.
	vec3 color = vec3(0.0);
	for (int x = -1; x <= 1; x++) {
		for (int y = -1; y <= 1; y++) {
			color += textureLod(screen_tex, SCREEN_UV + vec2(float(x), float(y)) * radius, lod).rgb;
		}
	}
	color /= 9.0;

	COLOR = vec4(color * (1.0 - dim * amount), 1.0);
}
