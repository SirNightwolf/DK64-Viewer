// Decompiled with JetBrains decompiler
//Edited by Nightwolf Prime using ChatGPT
// Type: DK64Viewer.Form1
// Assembly: DK64Viewer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 34C2999C-2061-412A-B2F3-E6D2C8F1D38B
// Assembly location: F:\DKViewer_0.04a\DK64Viewer.exe

using System;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace DK64Viewer
{
    public class Form1 : Form
    {
        private GLControl gl;
        private Timer renderTimer;

        // GL buffers
        private int vboPos = 0;
        private int vboCol = 0;
        private int vboUv = 0;
        private int ibo = 0;
        private int indexCount = 0;

        // Test texture (uses the Texture.cs we added)
        private Texture testTex;

        // Animation
        private float angle = 0f;
        private Matrix4 projection;

        public Form1()
        {
            Text = "DK64 Viewer (Textured + Vertex Colors demo)";
            Width = 1280;
            Height = 720;

            gl = new GLControl(new GraphicsMode(32, 24, 0, 0));
            gl.Dock = DockStyle.Fill;
            gl.Load += Gl_Load;
            gl.Paint += Gl_Paint;
            gl.Resize += Gl_Resize;
            Controls.Add(gl);

            renderTimer = new Timer { Interval = 16 };
            renderTimer.Tick += (s, e) => { angle += 0.6f; gl.Invalidate(); };
            renderTimer.Start();

            this.FormClosed += (s, e) => Cleanup();
        }

        private void Gl_Load(object sender, EventArgs e)
        {
            if (!gl.Context.IsCurrent) gl.MakeCurrent();

            GL.ClearColor(0.08f, 0.08f, 0.1f, 1f);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.Disable(EnableCap.CullFace); // DK64 often needs double-sided

            // Build a simple cube to verify the pipeline
            BuildCubeBuffers();
            BuildTestTexture();

            // Initial projection
            Gl_Resize(null, EventArgs.Empty);
        }

        private void Gl_Resize(object sender, EventArgs e)
        {
            if (gl.Width == 0 || gl.Height == 0) return;
            GL.Viewport(0, 0, gl.Width, gl.Height);
            float aspect = gl.Width / (float)gl.Height;
            projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(60f), aspect, 0.1f, 100f);
        }

        private void Gl_Paint(object sender, PaintEventArgs e)
        {
            if (!gl.Context.IsCurrent) gl.MakeCurrent();

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // --- Projection / View / Model (fixed-function for simplicity) ---
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projection);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.Translate(0f, 0f, -3.0f);
            GL.Rotate(angle, 0f, 1f, 0f);
            GL.Rotate(angle * 0.5f, 1f, 0f, 0f);

            // --- Enable arrays ---
            GL.EnableClientState(EnableCap.VertexArray);
            GL.EnableClientState(EnableCap.ColorArray);
            GL.EnableClientState(EnableCap.TextureCoordArray);

            // Bind buffers for arrays
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboPos);
            GL.VertexPointer(3, VertexPointerType.Float, 0, IntPtr.Zero);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vboCol);
            GL.ColorPointer(4, ColorPointerType.UnsignedByte, 0, IntPtr.Zero);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vboUv);
            GL.TexCoordPointer(2, TexCoordPointerType.Float, 0, IntPtr.Zero);

            // Texture state: texture * vertexColor (N64-style modulate)
            GL.Enable(EnableCap.Texture2D);
            GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvMode.Modulate);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // Bind texture
            GL.BindTexture(TextureTarget.Texture2D, testTex?.Id ?? 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            // Draw
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);
            if (indexCount > 0)
                GL.DrawElements(PrimitiveType.Triangles, indexCount, DrawElementsType.UnsignedShort, IntPtr.Zero);

            // Cleanup state
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.Disable(EnableCap.Texture2D);
            GL.Disable(EnableCap.Blend);

            GL.DisableClientState(EnableCap.TextureCoordArray);
            GL.DisableClientState(EnableCap.ColorArray);
            GL.DisableClientState(EnableCap.VertexArray);

            gl.SwapBuffers();
        }

        // --- Build a small demo geometry so you can verify texture * vertex color ---
        private void BuildCubeBuffers()
        {
            // 6 faces * 4 verts each = 24 verts, 12 triangles = 36 indices
            float[] pos =
            {
                // Front (z+)
                -1,-1, 1,   1,-1, 1,   1, 1, 1,  -1, 1, 1,
                // Back  (z-)
                1,-1,-1,  -1,-1,-1,  -1, 1,-1,   1, 1,-1,
                // Left  (x-)
                -1,-1,-1, -1,-1, 1,  -1, 1, 1,  -1, 1,-1,
                // Right (x+)
                 1,-1, 1,   1,-1,-1,  1, 1,-1,   1, 1, 1,
                // Top   (y+)
                -1, 1, 1,   1, 1, 1,   1, 1,-1, -1, 1,-1,
                // Bot   (y-)
                -1,-1,-1,  1,-1,-1,   1,-1, 1,  -1,-1, 1,
            };

            // UVs: simple 0..1 quad per face
            float[] uv =
            {
                0,0, 1,0, 1,1, 0,1,
                0,0, 1,0, 1,1, 0,1,
                0,0, 1,0, 1,1, 0,1,
                0,0, 1,0, 1,1, 0,1,
                0,0, 1,0, 1,1, 0,1,
                0,0, 1,0, 1,1, 0,1,
            };

            // Vertex colors (RGBA8). Mostly white to prove Modulate doesn’t wash out;
            // tweak a couple faces to show tinting works.
            byte[] col = new byte[24 * 4];
            for (int i = 0; i < 24; i++)
            {
                byte r = 255, g = 255, b = 255, a = 255;
                if (i < 4) { r = 255; g = 220; b = 220; } // front: slight red tint
                else if (i >= 8 && i < 12) { r = 220; g = 255; b = 220; } // left: slight green tint
                col[i * 4 + 0] = r;
                col[i * 4 + 1] = g;
                col[i * 4 + 2] = b;
                col[i * 4 + 3] = a;
            }

            // Indices: two triangles per face
            ushort[] idx = new ushort[36];
            int k = 0;
            for (ushort f = 0; f < 6; f++)
            {
                ushort o = (ushort)(f * 4);
                idx[k++] = (ushort)(o + 0); idx[k++] = (ushort)(o + 1); idx[k++] = (ushort)(o + 2);
                idx[k++] = (ushort)(o + 2); idx[k++] = (ushort)(o + 3); idx[k++] = (ushort)(o + 0);
            }
            indexCount = idx.Length;

            // Upload to GPU
            vboPos = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboPos);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(sizeof(float) * pos.Length), pos, BufferUsageHint.StaticDraw);

            vboUv = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboUv);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(sizeof(float) * uv.Length), uv, BufferUsageHint.StaticDraw);

            vboCol = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboCol);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(sizeof(byte) * col.Length), col, BufferUsageHint.StaticDraw);

            ibo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(sizeof(ushort) * idx.Length), idx, BufferUsageHint.StaticDraw);

            // Unbind
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }

        private void BuildTestTexture()
        {
            // 2x2 checkerboard RGBA, alpha 255
            byte[] rgba = {
                255,255,255,255,  64,64,64,255,
                 64,64,64,255,   255,255,255,255
            };
            testTex = Texture.FromRGBA8(rgba, 2, 2);
        }

        private void Cleanup()
        {
            if (testTex != null) { testTex.Dispose(); testTex = null; }
            if (vboPos != 0) { GL.DeleteBuffer(vboPos); vboPos = 0; }
            if (vboUv != 0) { GL.DeleteBuffer(vboUv); vboUv = 0; }
            if (vboCol != 0) { GL.DeleteBuffer(vboCol); vboCol = 0; }
            if (ibo != 0) { GL.DeleteBuffer(ibo); ibo = 0; }
        }
    }
}
