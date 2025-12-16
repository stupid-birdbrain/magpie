using Auklet.Core;
using Standard;
using System.Numerics;

namespace Magpie.Ext;

public static class CmdBufferExt {
    extension(CmdBuffer buf) {
        public void SetViewport(Rectangle rect, float minDepth = 0f, float maxDepth = 1f) 
            => buf.SetViewport(new Vector4(rect.X, rect.Y, rect.Width, rect.Height), minDepth, maxDepth);
    }
    
    extension(CmdBuffer buf) {
        public void SetScissor(Rectangle rect) 
            => buf.SetScissor(new Rectangle(rect.X, rect.Y, rect.Width, rect.Height));
    }
}