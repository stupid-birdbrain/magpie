using Auklet;
using Auklet.Core;

namespace Magpie.Rendering;

public readonly ref struct TextureView {
    public readonly Sampler Sampler;
    public readonly ImageView ImageView;
    public readonly uint MipLevel;

    public TextureView(Sampler sampler, ImageView imageView, uint mipLevel = 0) {
        Sampler = sampler;
        ImageView = imageView;
        MipLevel = mipLevel;
    }
}