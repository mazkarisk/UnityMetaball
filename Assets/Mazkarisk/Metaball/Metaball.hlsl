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

void Metaball_float(float3 CameraPosition, float3 CameraDirection, float3 FragmentPosition, out float Alpha) {
	const float3 rayDir = normalize(FragmentPosition - CameraPosition); // レイの進行方向

	Alpha = 0;

	// レイマーチング開始
	float3 pos = CameraPosition; // レイの座標(カメラのワールド座標から開始)
	float totalDistance = 0; // 合計行進距離
	for (int marchCount = 0; marchCount < MAX_MARCH_COUNT; marchCount++) {
		const float distance = getDistance(pos);

		// 距離が閾値以下になったらヒットしたとみなして処理終了
		if (distance <= HIT_THRESHOLD) {
			Alpha = 1;
			break;
		}

		// レイの方向に行進
		pos += distance * rayDir;
		totalDistance += distance;
	}

	return;
}

#endif
