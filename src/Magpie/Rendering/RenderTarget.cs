using Auklet;
using Auklet.Core;
using Magpie.Rendering;
using Vortice.Vulkan;

namespace Magpie;

public sealed class RenderTarget : IDisposable {
    public Image Image { get; }
    public DeviceMemory Memory { get; }
    public ImageView ImageView { get; }
    public uint Width { get; }
    public uint Height { get; }
    public VkFormat Format { get; }
    internal VkImageLayout CurrentLayout { get; set; }

    private readonly LogicalDevice _device;
    private readonly CmdPool _commandPool;
    private readonly Queue _queue;

    internal RenderTarget(LogicalDevice device, CmdPool commandPool, Queue queue, uint width, uint height, VkFormat format) {
        _device = device;
        _commandPool = commandPool;
        _queue = queue;

        Width = width;
        Height = height;
        Format = format;

        Image = new Image(device, width, height, 1, format, VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.Sampled | VkImageUsageFlags.TransferDst | VkImageUsageFlags.TransferSrc);
        Memory = new DeviceMemory(Image, VkMemoryPropertyFlags.DeviceLocal);
        ImageView = new ImageView(Image);

        using var fence = new Fence(device, VkFenceCreateFlags.None);
        var cmd = commandPool.CreateCommandBuffer();
        cmd.Begin();
        cmd.TransitionImageLayout(Image, VkImageLayout.Undefined, VkImageLayout.ShaderReadOnlyOptimal);
        cmd.End();
        queue.Submit(cmd, fence);
        fence.Wait();
        cmd.Dispose();

        CurrentLayout = VkImageLayout.ShaderReadOnlyOptimal;
    }

    public Texture2D CreateTexture2D(VkFilter filter = VkFilter.Linear, VkSamplerAddressMode addressMode = VkSamplerAddressMode.ClampToEdge) {
        Sampler sampler = new(_device, new SamplerCreateParameters(filter, addressMode));
        return new Texture2D(
            _device,
            _commandPool,
            _queue,
            Image,
            Memory,
            ImageView,
            sampler,
            ownsImage: false,
            ownsMemory: false,
            ownsImageView: false,
            ownsSampler: true
        );
    }

    public void Dispose() {
        ImageView.Dispose();
        Memory.Dispose();
        Image.Dispose();
    }
}