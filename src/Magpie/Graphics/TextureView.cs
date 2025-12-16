using Auklet;
using Auklet.Core;

namespace Magpie.Graphics;

public readonly ref struct TextureView {
    public readonly Sampler Sampler;
    public readonly ImageView Image;

    public readonly int MipLevel;
}