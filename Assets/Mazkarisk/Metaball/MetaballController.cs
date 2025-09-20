using UnityEngine;

[ExecuteAlways]
public class MetaballController : MonoBehaviour {

	private const int MAX_SPHERE_COUNT = 256; // 球の最大個数（シェーダー側と合わせる）
	private readonly Vector4[] _spheres = new Vector4[MAX_SPHERE_COUNT];
	ComputeBuffer spheresBuffer = null;

	[SerializeField]
	private Material material = null;

	[SerializeField, Range(1, 256)]
	int maxMarchCount = 256;
	[SerializeField, Range(0.0001f, 0.01f)]
	float hitThreshold = 0.001f;
	[SerializeField]
	bool debugViewEnable = false;

	void Start() {
		// 球の設定
		for (var i = 0; i < MAX_SPHERE_COUNT; i++) {
			Vector2 randomInsideUnitCircle = Random.insideUnitCircle;
			// 中心座標と半径を格納
			_spheres[i] = new Vector4(randomInsideUnitCircle.x * 2f, Random.Range(0f, 2f), randomInsideUnitCircle.y * 2f, Random.Range(0.1f, 0.2f));
		}

		int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector4));
		spheresBuffer = new ComputeBuffer(MAX_SPHERE_COUNT, stride, ComputeBufferType.Default);
		spheresBuffer.SetData(_spheres);
	}

	void Update() {
		material.SetInt("_SphereCount", 40);
		material.SetBuffer("spheresBuffer", spheresBuffer);
		material.SetInt("_DebugViewEnable", debugViewEnable ? 1 : 0);
		material.SetFloat("_MaxMarchDistance", Camera.main.farClipPlane * 0.5f);
		material.SetFloat("_MaxMarchCount", maxMarchCount);
		material.SetFloat("_HitThreshold", hitThreshold);
	}
}

