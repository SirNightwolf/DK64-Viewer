// Decompiled with JetBrains decompiler
//Edited by NightwolfPrime with some help from ChatGPT
// Type: DK64Viewer.Texture
// Assembly: DK64Viewer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 34C2999C-2061-412A-B2F3-E6D2C8F1D38B
// Assembly location: F:\DKViewer_0.04a\DK64Viewer.exe

using System;
using OpenTK.Graphics.OpenGL;

namespace DK64Viewer
{
    public enum N64TexFormat { RGBA16, IA8, IA16, I4, I8, CI4, CI8 }

    public sealed class Texture : IDisposable
    {
        public int Id { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        private Texture() { }
        public void Dispose() { if (Id != 0) { GL.DeleteTexture(Id); Id = 0; } }

        public static Texture FromRGBA8(byte[] rgba, int width, int height, bool clampS=false, bool clampT=false)
        {
            if (rgba == null) throw new ArgumentNullException(nameof(rgba));
            if (rgba.Length != width * height * 4) throw new ArgumentException("RGBA buffer length must be width*height*4.");
            var tex = new Texture { Width = width, Height = height };
            tex.Id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, tex.Id);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)(clampS ? TextureWrapMode.Clamp : TextureWrapMode.Repeat));
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)(clampT ? TextureWrapMode.Clamp : TextureWrapMode.Repeat));
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, rgba);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            return tex;
        }

        public static Texture FromN64Bytes(N64TexFormat format, byte[] texData, int width, int height,
                                           byte[] tlutRgba16=null, bool clampS=false, bool clampT=false)
        {
            if (texData == null) throw new ArgumentNullException(nameof(texData));
            if (width<=0 || height<=0) throw new ArgumentOutOfRangeException("Invalid width/height.");
            byte[] rgba = format switch
            {
                N64TexFormat.RGBA16 => DecodeRGBA16(texData),
                N64TexFormat.IA8    => DecodeIA8(texData),
                N64TexFormat.IA16   => DecodeIA16(texData),
                N64TexFormat.I4     => DecodeI4(texData),
                N64TexFormat.I8     => DecodeI8(texData),
                N64TexFormat.CI4    => DecodeCI4(texData, tlutRgba16),
                N64TexFormat.CI8    => DecodeCI8(texData, tlutRgba16),
                _ => throw new NotSupportedException()
            };
            if (rgba.Length != width * height * 4) throw new InvalidOperationException("Decoded buffer size mismatch.");
            return FromRGBA8(rgba, width, height, clampS, clampT);
        }

        private static byte[] DecodeRGBA16(byte[] src)
        {
            if (src.Length % 2 != 0) throw new ArgumentException("RGBA16 length must be even.");
            int pix = src.Length / 2;
            var outp = new byte[pix * 4]; int o=0;
            for (int i=0;i<src.Length;i+=2)
            {
                ushort p = (ushort)((src[i]<<8)|src[i+1]);
                byte r=(byte)(((p>>11)&0x1F)*255/31);
                byte g=(byte)(((p>>6)&0x1F)*255/31);
                byte b=(byte)(((p>>1)&0x1F)*255/31);
                byte a=(byte)((p&1)!=0?255:0);
                outp[o++]=b; outp[o++]=g; outp[o++]=r; outp[o++]=a;
            }
            return outp;
        }

        private static byte[] DecodeIA8(byte[] src)
        {
            var outp=new byte[src.Length*4]; int o=0;
            for(int i=0;i<src.Length;i++)
            {
                byte b=src[i]; byte i4=(byte)((b>>4)&0xF); byte a4=(byte)(b&0xF);
                byte I=(byte)(i4*17); byte A=(byte)(a4*17);
                outp[o++]=I; outp[o++]=I; outp[o++]=I; outp[o++]=A;
            }
            return outp;
        }

        private static byte[] DecodeIA16(byte[] src)
        {
            if (src.Length%2!=0) throw new ArgumentException("IA16 length must be even.");
            var outp=new byte[(src.Length/2)*4]; int o=0;
            for (int i=0;i<src.Length;i+=2)
            {
                byte I=src[i], A=src[i+1];
                outp[o++]=I; outp[o++]=I; outp[o++]=I; outp[o++]=A;
            }
            return outp;
        }

        private static byte[] DecodeI4(byte[] src)
        {
            var outp=new byte[src.Length*2*4]; int o=0;
            for(int i=0;i<src.Length;i++)
            {
                byte b=src[i]; byte hi=(byte)((b>>4)&0xF), lo=(byte)(b&0xF);
                byte Ihi=(byte)(hi*17), Ilo=(byte)(lo*17);
                outp[o++]=Ihi; outp[o++]=Ihi; outp[o++]=Ihi; outp[o++]=255;
                outp[o++]=Ilo; outp[o++]=Ilo; outp[o++]=Ilo; outp[o++]=255;
            }
            return outp;
        }

        private static byte[] DecodeI8(byte[] src)
        {
            var outp=new byte[src.Length*4]; int o=0;
            for(int i=0;i<src.Length;i++)
            {
                byte I=src[i];
                outp[o++]=I; outp[o++]=I; outp[o++]=I; outp[o++]=255;
            }
            return outp;
        }

        private static byte[] DecodeCI4(byte[] tex, byte[] tlutRGBA16)
        {
            if (tlutRGBA16==null || tlutRGBA16.Length==0) throw new ArgumentNullException(nameof(tlutRGBA16));
            byte[] pal = DecodeRGBA16(tlutRGBA16);
            var outp=new byte[tex.Length*2*4]; int o=0;
            for(int i=0;i<tex.Length;i++)
            {
                byte b=tex[i]; int hi=(b>>4)&0xF, lo=b&0xF;
                int p=hi*4; outp[o++]=pal[p+0]; outp[o++]=pal[p+1]; outp[o++]=pal[p+2]; outp[o++]=pal[p+3];
                p=lo*4;    outp[o++]=pal[p+0]; outp[o++]=pal[p+1]; outp[o++]=pal[p+2]; outp[o++]=pal[p+3];
            }
            return outp;
        }

        private static byte[] DecodeCI8(byte[] tex, byte[] tlutRGBA16)
        {
            if (tlutRGBA16==null || tlutRGBA16.Length==0) throw new ArgumentNullException(nameof(tlutRGBA16));
            byte[] pal=DecodeRGBA16(tlutRGBA16);
            var outp=new byte[tex.Length*4]; int o=0;
            for(int i=0;i<tex.Length;i++)
            {
                int idx=tex[i]*4;
                outp[o++]=pal[idx+0]; outp[o++]=pal[idx+1]; outp[o++]=pal[idx+2]; outp[o++]=pal[idx+3];
            }
            return outp;
        }
    }
}
