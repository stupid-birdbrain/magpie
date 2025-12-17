using Auklet;
using Auklet.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Runtime.CompilerServices;
using Vortice.Vulkan;
using Buffer = Auklet.Core.Buffer;
using Image = Auklet.Core.Image;

namespace Magpie.Rendering;

/* lots todo here */

public class Texture2D {
    private readonly LogicalDevice _device;
    private readonly CmdPool _cmdPool;
    private readonly Queue _queue;

    private Image _image;
    private DeviceMemory _memory;
    private ImageView _imageView;
    private Sampler _sampler;

    private readonly bool _ownsImage;
    private readonly bool _ownsMemory;
    private readonly bool _ownsImageView;
    private readonly bool _ownsSampler;
    private bool _disposed;

    public uint Width => _image.Width;
    public uint Height => _image.Height;
    public VkFormat Format => _image.Format;
    
    public TextureView View => new(_sampler, _imageView);
    
    internal Texture2D(
        LogicalDevice device,
        CmdPool cmdPool,
        Queue queue,
        Image image,
        DeviceMemory memory,
        ImageView imageView,
        Sampler sampler,
        bool ownsImage = true,
        bool ownsMemory = true,
        bool ownsImageView = true,
        bool ownsSampler = true) {
        _device = device;
        _cmdPool = cmdPool;
        _queue = queue;

        _image = image;
        _memory = memory;
        _imageView = imageView;
        _sampler = sampler;
        _ownsImage = ownsImage;
        _ownsMemory = ownsMemory;
        _ownsImageView = ownsImageView;
        _ownsSampler = ownsSampler;
    }
    
    public static Texture2D FromFile(
        GraphicsDevice graphicsDevice,
        string path,
        VkFilter filter = VkFilter.Linear,
        VkSamplerAddressMode addressMode = VkSamplerAddressMode.Repeat) {
        if (graphicsDevice == null) throw new ArgumentNullException(nameof(graphicsDevice));
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("path cannot be null or empty!", nameof(path));

        using Image<Rgba32> imageSharp = SixLabors.ImageSharp.Image.Load<Rgba32>(path);
        imageSharp.Mutate(x => x.Flip(FlipMode.Vertical));

        uint imageSize = (uint)(imageSharp.Width * imageSharp.Height * Unsafe.SizeOf<Rgba32>());
        var pixelData = new byte[imageSize];
        imageSharp.CopyPixelDataTo(pixelData);
        ReadOnlySpan<byte> pixelSpan = pixelData;

        using var stagingBuffer = new Buffer(graphicsDevice.LogicalDevice, imageSize, VkBufferUsageFlags.TransferSrc);
        using var stagingMemory = new DeviceMemory(stagingBuffer, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent);
        stagingMemory.CopyFrom(pixelSpan);

        Image vkImage = new(
            graphicsDevice.LogicalDevice,
            (uint)imageSharp.Width,
            (uint)imageSharp.Height,
            1,
            VkFormat.R8G8B8A8Unorm,
            VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled | VkImageUsageFlags.TransferSrc
        );
        DeviceMemory vkMemory = new(vkImage, VkMemoryPropertyFlags.DeviceLocal);
        ImageView vkImageView = new(vkImage);

        Sampler vkSampler = new(graphicsDevice.LogicalDevice, new SamplerCreateParameters(filter, addressMode));

        using var fence = graphicsDevice.RequestFence(VkFenceCreateFlags.None);
        CmdBuffer cmd = graphicsDevice.GraphicsCommandPool.CreateCommandBuffer(isPrimary: true);
        cmd.Begin();
        cmd.TransitionImageLayout(vkImage, VkImageLayout.Undefined, VkImageLayout.TransferDstOptimal);
        cmd.CopyBufferToImage(stagingBuffer, vkImage, (uint)imageSharp.Width, (uint)imageSharp.Height, 0, 0);
        cmd.TransitionImageLayout(vkImage, VkImageLayout.TransferDstOptimal, VkImageLayout.ShaderReadOnlyOptimal);
        cmd.End();

        graphicsDevice.GraphicsQueue.Submit(cmd, fence);
        fence.Wait();
        cmd.Dispose();

        return new Texture2D(graphicsDevice.LogicalDevice, graphicsDevice.GraphicsCommandPool, graphicsDevice.GraphicsQueue, vkImage, vkMemory, vkImageView, vkSampler);
    }
    
    public void Dispose() {
        if (_disposed) {
            return;
        }

        if (_ownsSampler) {
            _sampler.Dispose();
        }
        if (_ownsImageView) {
            _imageView.Dispose();
        }
        if (_ownsMemory) {
            _memory.Dispose();
        }
        if (_ownsImage) {
            _image.Dispose();
        }
        _disposed = true;
    }
}