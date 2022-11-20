#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;
using System.Drawing;
using System;
using System.Drawing.Imaging;
using UnityEditor;
using System.Runtime.InteropServices;

namespace Zanthous 
{
	public enum TilemapImageFormat
	{
		png,
		bmp,
		jpg
	}

	[CustomEditor(typeof(TilemapToImage))]
	public class TilemapToImageEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			TilemapToImage tilemapToPng = (TilemapToImage) target;

			if(GUILayout.Button("Generate"))
			{
				tilemapToPng.GenerateSprite();
			}
		}
	}


	public class TilemapToImage : MonoBehaviour
	{
		[SerializeField] string fileName = "";
		[SerializeField] bool compressBounds = true;
		[SerializeField] int pixelResolution = 32;
		[SerializeField] TilemapImageFormat format;
		Tilemap tm;

		int minX = 0;
		int maxX = 0;
		int minY = 0;
		int maxY = 0;

		Color32[] colors;

		public DirectBitmap bmp;
		public DirectBitmap overlay;

		public void GenerateSprite()
		{
			tm = GetComponent<Tilemap>();

			if(compressBounds)
				tm.CompressBounds();

			maxX = tm.cellBounds.xMax;
			minX = tm.cellBounds.xMin;
			maxY = tm.cellBounds.yMax;
			minY = tm.cellBounds.yMin;

			bmp = new DirectBitmap(pixelResolution * tm.size.x, pixelResolution * tm.size.y);

			//assign to each block its respective pixels
			for(int x = minX; x <= maxX; x++)
			{
				for(int y = minY; y <= maxY; y++)
				{
					if(tm.GetSprite(new Vector3Int(x, y, 0)) != null)
					{
						//map the pixels so that the minX = 0 y minY = 0
						var t = tm.GetTransformMatrix(new Vector3Int(x, y, 0));

						SetColors(
							(x - minX) * pixelResolution, (y - minY) * pixelResolution,
							pixelResolution,
							pixelResolution,
							GetCurrentSprite(tm.GetSprite(new Vector3Int(x, y, 0)), t).GetPixels32());
					}
				}
			}


			ExportImage(fileName == "" ? name : fileName, format);
		}
		//Copy the color32 values into our temporary array (these will be copied over to the Bitmap afterward)
		void SetColors(int x, int y, int width, int height, Color32[] colors_in)
		{
			for(int i = 0; i < height; i++)
			{
				for(int j = 0; j < width; j++)
				{
					var col = colors_in[(height - i - 1) * width + j];
					bmp.SetPixel(j + x, bmp.Height - pixelResolution - y + i, new Color32(col.b, col.g, col.r, col.a));
				}
			}
		}

		Texture2D GetCurrentSprite(Sprite sprite, Matrix4x4 m) //retrieve sprite for this tile and transform it if necessary 
		{
			var pixels = sprite.texture.GetPixels((int) sprite.textureRect.x,
											 (int) sprite.textureRect.y,
											 (int) sprite.textureRect.width,
											 (int) sprite.textureRect.height);

			Texture2D texture = new Texture2D((int) sprite.textureRect.width,
											 (int) sprite.textureRect.height);
			texture.SetPixels(pixels);

			//Rotate/flip the pixels of tiles that are rotated/flipped. Slow and potentially unnecessary calls of apply
			//but I don't know what the best way to do it is and I don't need it to be fast
			if(Mathf.Approximately(m.rotation.eulerAngles.z, -90.0f) || Mathf.Approximately(m.rotation.eulerAngles.z, 270.0f))
			{
				Texture2D temp = new Texture2D(texture.width, texture.height);
				for(int y = 0; y < temp.height; y++)
				{
					for(int x = 0; x < temp.width; x++)
					{
						temp.SetPixel(x, y, texture.GetPixel(temp.width - y - 1, x));
					}
				}
				texture.SetPixels32(temp.GetPixels32());
			}
			if(Mathf.Approximately(m.rotation.eulerAngles.z, 90.0f) || Mathf.Approximately(m.rotation.eulerAngles.z, -270.0f))
			{
				Texture2D temp = new Texture2D(texture.width, texture.height);
				for(int y = 0; y < temp.height; y++)
				{
					for(int x = 0; x < temp.width; x++)
					{
						temp.SetPixel(x, y, texture.GetPixel(y, temp.height - x - 1));
					}
				}
				texture.SetPixels32(temp.GetPixels32());
			}
			if(Mathf.Approximately(m.rotation.eulerAngles.z, 180.0f))
			{
				Texture2D temp = new Texture2D(texture.width, texture.height);
				for(int y = 0; y < temp.height; y++)
				{
					for(int x = 0; x < temp.width; x++)
					{
						temp.SetPixel(x, y, texture.GetPixel(temp.height - x - 1, temp.width - y - 1));
					}
				}
				texture.SetPixels32(temp.GetPixels32());
			}
			if(Mathf.Approximately(m.lossyScale.x, -1.0f) || Mathf.Approximately(m.rotation.eulerAngles.y, 180.0f))
			{
				Texture2D temp = new Texture2D(texture.width, texture.height);
				for(int y = 0; y < temp.height; y++)
				{
					for(int x = 0; x < temp.width; x++)
					{
						temp.SetPixel(x, y, texture.GetPixel(temp.width - x - 1, y));
					}
				}
				texture.SetPixels32(temp.GetPixels32());
			}

			return texture;
		}

		void ExportImage(string fileName, TilemapImageFormat format)
		{
			var dirPath = Application.dataPath + "/Tilemap Images/";
			if(!Directory.Exists(dirPath))
			{
				Directory.CreateDirectory(dirPath);
			}
			ImageFormat imageFormat = ImageFormat.Png;
			var ext = "";
			//add more here if you want
			switch(format)
			{
				case TilemapImageFormat.png:
					imageFormat = ImageFormat.Png;
					ext = ".png";
					break;
				case TilemapImageFormat.bmp:
					imageFormat = ImageFormat.Bmp;
					ext = ".bmp";
					break;
				case TilemapImageFormat.jpg:
					imageFormat = ImageFormat.Jpeg;
					ext = ".jpg";
					break;
			}
			var fullPath = dirPath + fileName + ext;
			bmp.Bitmap.Save(fullPath, imageFormat);
			Debug.Log("Saved to " + fullPath);
			Debug.Log("Refresh your assets with ctrl + R or similar");
			bmp = null;
		}
	}
}

public class DirectBitmap : IDisposable
{
	public Bitmap Bitmap { get; private set; }
	public Color32[] Colors { get; private set; }
	public bool Disposed { get; private set; }
	public int Height { get; private set; }
	public int Width { get; private set; }

	protected GCHandle BitsHandle { get; private set; }

	public DirectBitmap(int width, int height)
	{
		Width = width;
		Height = height;
		Colors = new Color32[width * height];
		BitsHandle = GCHandle.Alloc(Colors, GCHandleType.Pinned);
		Bitmap = new Bitmap(width, height, width * 4, PixelFormat.Format32bppArgb, BitsHandle.AddrOfPinnedObject());
	}

	public void SetPixel(int x, int y, Color32 colour)
	{
		Colors[x + (y * Width)] = colour;
	}

	public Color32 GetPixel(int x, int y)
	{
		return Colors[x + (y * Width)];
	}

	public void Dispose()
	{
		if(Disposed) return;
		Disposed = true;
		Bitmap.Dispose();
		BitsHandle.Free();
	}
}
#endif