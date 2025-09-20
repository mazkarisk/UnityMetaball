#ifndef METABALL_INCLUDED
#define METABALL_INCLUDED

#define MAX_MARCH_COUNT 100	// レイの最大行進回数
#define HIT_THRESHOLD 0.01	// レイの衝突判定距離


// 球の距離関数
float sphereDistanceFunction(float4 sphere, float3 pos) {
	return length(sphere.xyz - pos) - sphere.w;
}

// 球との最短距離を返す
float getDistance(float3 pos) {
	return sphereDistanceFunction(float4(0, 0, 0, 1), pos);
}

// 指定位置の法線を偏微分により取得
float3 getNormal(const float3 pos) {
	const float delta = HIT_THRESHOLD;
	const float x = getDistance(pos + float3(delta, 0.0, 0.0)) - getDistance(pos + float3(-delta, 0.0, 0.0));
	const float y = getDistance(pos + float3(0.0, delta, 0.0)) - getDistance(pos + float3(0.0, -delta, 0.0));
	const float z = getDistance(pos + float3(0.0, 0.0, delta)) - getDistance(pos + float3(0.0, 0.0, -delta));
	return normalize(float3(x, y, z));
}


/****************************************/
/* Shader Graphからのエントリーポイント */
/****************************************/

void Metaball_float(float3 CameraPosition, float3 CameraDirection, float3 FragmentPosition, float4x4 ViewMatrix, out float Alpha, out float DepthOffset, out float3 Normal) {
	const float3 rayDir = normalize(FragmentPosition - CameraPosition); // レイの進行方向

	// デフォルト値の設定
	Alpha = 0;
	DepthOffset = 0;
	Normal = normalize(CameraPosition - FragmentPosition);

	// レイマーチング開始
	float3 pos = CameraPosition; // レイの座標(カメラのワールド座標から開始)
	float totalDistance = 0; // 合計行進距離
	for (int marchCount = 0; marchCount < MAX_MARCH_COUNT; marchCount++) {
		const float distance = getDistance(pos);

		// 距離が閾値以下になったらヒットしたとみなして処理終了
		if (distance <= HIT_THRESHOLD) {
			Alpha = 1;
			Normal = getNormal(pos);
			break;
		}

		// レイの方向に行進
		pos += distance * rayDir;
		totalDistance += distance;
	}
	
	const float sceneDepthDistanceRate = 1 / dot(rayDir, CameraDirection);
	DepthOffset = (mul(ViewMatrix, float4(FragmentPosition.xyz, 1)).z - mul(ViewMatrix, float4(pos.xyz, 1)).z) * sceneDepthDistanceRate;

	return;
}

#endif
