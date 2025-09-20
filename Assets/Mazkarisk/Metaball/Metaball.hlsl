#ifndef METABALL_INCLUDED
#define METABALL_INCLUDED

#define MAX_SPHERE_COUNT 256 // 球の最大数

StructuredBuffer<float4> spheresBuffer; // 球の位置情報(x:x座標, y:y座標, z:z座標, w:半径)

int _SphereCount; // 処理対象となる球の個数
float _MaxMarchDistance; // レイの最大行進距離(スクリプトから"カメラから最も遠い球の向こう側までの距離"を設定することを想定)
int _MaxMarchCount; // レイの最大行進回数
float _HitThreshold; // レイの衝突判定閾値(レイの先端とメタボールの距離がこの値以下になったら"ヒット"したと判定する)

int _DebugViewEnable; // デバッグ表示(0:しない, 1:更新回数を表示)

// 球の距離関数
float sphereDistanceFunction(float4 sphere, float3 pos) {
	return length(sphere.xyz - pos) - sphere.w;
}

// 球との最短距離を返す
float getDistance(float3 pos) {
	float distance = _MaxMarchDistance; // 初期値として十分に遠い距離を設定
	for (int i = 0; i < _SphereCount; i++) {
		distance = min(distance, sphereDistanceFunction(spheresBuffer[i], pos));
	}
	return distance;
}

// 指定位置の法線を偏微分により取得
float3 getNormal(const float3 pos) {
	const float delta = _HitThreshold;
	const float x = getDistance(pos + float3(delta, 0, 0)) - getDistance(pos + float3(-delta, 0, 0));
	const float y = getDistance(pos + float3(0, delta, 0)) - getDistance(pos + float3(0, -delta, 0));
	const float z = getDistance(pos + float3(0, 0, delta)) - getDistance(pos + float3(0, 0, -delta));
	return normalize(float3(x, y, z));
}


/****************************************/
/* Shader Graphからのエントリーポイント */
/****************************************/

void Metaball_float(
		float3 CameraPosition,
		float3 CameraDirection,
		float3 FragmentPosition,
		float SceneDepth,
		float4x4 ViewMatrix,
		out float3 BaseColor,
		out float Alpha,
		out float DepthOffset,
		out float3 Normal) {

	const float3 rayDir = normalize(FragmentPosition - CameraPosition); // レイの進行方向
	
	// 深度補正値の計算
	const float sceneDepthDistanceRate = 1 / dot(rayDir, CameraDirection);
	const float sceneDepthDistance = SceneDepth * sceneDepthDistanceRate;

	// デフォルト値の設定
	BaseColor = float3(1, 1, 1); // 色は白
	Alpha = 1; // アルファクリッピングは無し
	DepthOffset = 0; // 深度オフセットは無し
	Normal = normalize(CameraPosition - FragmentPosition); // 法線はカメラに対して垂直

	// レイマーチング準備
	float3 pos = CameraPosition; // レイの座標(カメラのワールド座標から開始)
	float totalDistance = 0; // 合計行進距離
	bool hit = false; // レイがヒットしたかどうかのフラグ、ヒットしなければfalseのまま
	int marchCount = 0;
	
	// レイマーチング開始
	for (marchCount = 1; marchCount <= _MaxMarchCount; marchCount++) {
		const float distance = getDistance(pos);

		// 距離が閾値以下になったらヒットしたとみなして処理終了
		if (distance <= _HitThreshold) {
			hit = true;
			break;
		}

		// 合計行進距離が描画済み深度に到達したら強制終了
		if (totalDistance >= sceneDepthDistance) {
			if (_DebugViewEnable) {
				const float marchRate = (float)marchCount / _MaxMarchCount;
				BaseColor = float3(0, 0, marchRate); // 青色で回数を表示
			} else {
				Alpha = 0; // アルファを0にしてクリップさせる
			}
			return;
		}

		// 合計行進距離が最大描画距離に到達したら強制終了
		if (totalDistance >= _MaxMarchDistance) {
			if (_DebugViewEnable) {
				const float marchRate = (float)marchCount / _MaxMarchCount;
				BaseColor = float3(0, marchRate, marchRate); // 水色で回数を表示
			} else {
				Alpha = 0; // アルファを0にしてクリップさせる
			}
			return;
		}

		// レイの方向に行進
		pos += distance * rayDir;
		totalDistance += distance;
	}
	
	// 結果の表示
	if (_DebugViewEnable) {
		const float marchRate = (float)marchCount / _MaxMarchCount;
		Alpha = 1;
		if (hit) {
			BaseColor = float3(0, marchRate, 0); // ヒットした場合は緑色で回数表示
		} else {
			BaseColor = float3(marchRate, 0, 0); // 最後までヒットしなかった場合は赤色で回数表示
		}
	} else {
		if (hit) {
			DepthOffset = (mul(ViewMatrix, float4(FragmentPosition.xyz, 1)).z - mul(ViewMatrix, float4(pos.xyz, 1)).z) * sceneDepthDistanceRate;
			Normal = getNormal(pos);
		} else {
			Alpha = 0; // アルファを0にしてクリップさせる
		}
	}

	return;
}

#endif
