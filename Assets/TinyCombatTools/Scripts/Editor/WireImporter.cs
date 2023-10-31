using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

[ScriptedImporter(1, "wire")]
public class WireImporter : ScriptedImporter {

	public override void OnImportAsset(AssetImportContext ctx) {
		Mesh tempMesh = CreateMeshFromLines(StreamFromString(File.ReadAllText(ctx.assetPath)));
		ctx.AddObjectToAsset(Path.GetFileNameWithoutExtension(ctx.assetPath), tempMesh);
		ctx.SetMainObject(tempMesh);
	}

	TextReader StreamFromString(string text) {
		return new StringReader(text);
	}

	Mesh CreateMeshFromLines(TextReader stream) {
		// Storage
		List<Vector3> vertices = new List<Vector3>();
		List<int> lines = new List<int>(); // Every 2 is one line, 
		List<Vector2> UVs = new List<Vector2>();
		List<Vector3> normals = new List<Vector3>();
		List<Color> colors = new List<Color>();

		// Read the file
		string line;
		while ((line = stream.ReadLine()) != null) {

			string[] words = line.Split(new string[] { " ", "\t" }, System.StringSplitOptions.None);
			if (words[0].Trim() == "v") {
				vertices.Add(
					new Vector3(
						float.Parse(words[1].Trim()) * -1f, // Blender to Unity swizzle
						float.Parse(words[3].Trim()),
						float.Parse(words[2].Trim()) * -1f
					)
				);
			} else if (words[0].Trim() == "vn") {
				normals.Add(
					new Vector3(
						float.Parse(words[1].Trim()) * -1f, // Blender to Unity swizzle
						float.Parse(words[3].Trim()),
						float.Parse(words[2].Trim()) * -1f
					)
				);
			} else if (words[0].Trim() == "vt") {
				UVs.Add(
					new Vector3(
						float.Parse(words[1].Trim()),
						float.Parse(words[2].Trim())
					)
				);
			} else if (words[0].Trim() == "vc") {
				colors.Add(
					new Color(
						float.Parse(words[1].Trim()),
						float.Parse(words[2].Trim()),
						float.Parse(words[3].Trim())
					)
				);
			} else if (words[0].Trim() == "l") {
				lines.Add(int.Parse(words[1].Trim()));
				lines.Add(int.Parse(words[2].Trim()));
			}
		}

		// Build the mesh
		Mesh mesh = new Mesh();
		mesh.vertices = vertices.ToArray();

		if (normals.Count == vertices.Count) {
			mesh.normals = normals.ToArray();
		}
		else {
			var normalArray = new Vector3[vertices.Count];
			for (int i = 0; i < vertices.Count; i++) {
				normalArray[i] = Vector3.up;
			}
			mesh.normals = normalArray;
		}

		if (UVs.Count == vertices.Count) {
			mesh.uv = UVs.ToArray();
		}

		if (colors.Count == vertices.Count) {
			mesh.colors = colors.ToArray();
		}

		// Assume we're using Blender's obj exporter which uses 1-based indexing
		if (UVs.Count != vertices.Count && normals.Count != vertices.Count) {
			for (int i = 0; i < lines.Count; i++) {
				lines[i] -= 1;
			}
		}

		mesh.SetIndices(lines.ToArray(), MeshTopology.Lines, 0);

		return mesh;
	}
}
