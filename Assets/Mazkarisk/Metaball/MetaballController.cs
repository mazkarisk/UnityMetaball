using UnityEngine;

[ExecuteAlways]
public class MetaballController : MonoBehaviour {

	private const int MAX_SPHERE_COUNT = 1024; // ���̍ő���i�V�F�[�_�[���ƍ��킹��j

	[SerializeField]
	private Material material = null;

	[SerializeField]
	float smoothWidth = 0.1f;
	[SerializeField, Range(1, 256)]
	int maxMarchCount = 256;
	[SerializeField]
	float hitThreshold = 0.001f;
	[SerializeField]
	bool debugViewEnable = false;

	private readonly Vector4[] _spheres = new Vector4[MAX_SPHERE_COUNT];
	ComputeBuffer spheresBuffer = null;

	void Start() {
		GetComponent<MeshFilter>().sharedMesh = CreateMeshForFullScreenEffect();
		GetComponent<MeshRenderer>().material = material;

		// ���̐ݒ�
		for (var i = 0; i < MAX_SPHERE_COUNT; i++) {
			Vector2 randomInsideUnitCircle = Random.insideUnitCircle;
			// ���S���W�Ɣ��a���i�[
			_spheres[i] = new Vector4(randomInsideUnitCircle.x * 2f, Random.Range(0f, 1f), randomInsideUnitCircle.y * 2f, Random.Range(0.10f, 0.10f));
		}

		int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector4));
		spheresBuffer = new ComputeBuffer(MAX_SPHERE_COUNT, stride, ComputeBufferType.Default);
		spheresBuffer.SetData(_spheres);
	}

	void Update() {
		material.SetInt("_SphereCount", 200);
		material.SetFloat("_SmoothWidth", smoothWidth);
		material.SetFloat("_MaxMarchCount", maxMarchCount);
		material.SetFloat("_MaxMarchDistance", Camera.main.farClipPlane * 0.5f);
		material.SetFloat("_HitThreshold", hitThreshold);
		material.SetInt("_DebugViewEnable", debugViewEnable ? 1 : 0);
		material.SetBuffer("_SpheresBuffer", spheresBuffer);
	}

	/// <summary>
	/// �t���X�N���[���G�t�F�N�g�p���b�V����V�K�쐬����B
	/// </summary>
	/// <returns>�V�K�쐬���ꂽ�t���X�N���[���G�t�F�N�g�p���b�V��</returns>
	private Mesh CreateMeshForFullScreenEffect() {
		Vector3[] vertices = new Vector3[4];
		vertices[0] = new Vector3(-1, -1, 0);
		vertices[1] = new Vector3(1, -1, 0);
		vertices[2] = new Vector3(-1, 1, 0);
		vertices[3] = new Vector3(1, 1, 0);

		Vector2[] uv = new Vector2[4];
		uv[0] = new Vector2(0, 1);
		uv[1] = new Vector2(1, 1);
		uv[2] = new Vector2(0, 0);
		uv[3] = new Vector2(1, 0);

		int[] triangles = new int[] { 0, 1, 2, 2, 1, 3 };

		Mesh mesh = new Mesh();
		mesh.vertices = vertices;
		mesh.uv = uv;
		mesh.triangles = triangles;
		mesh.bounds = new Bounds(Vector3.zero, Vector3.one * float.MaxValue);
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();

		return mesh;
	}
}
