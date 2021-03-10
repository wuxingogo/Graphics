using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class SetTextureOverridePass : CustomPass
{
    // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
    // When empty this render pass will render to the active camera render target.
    // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
    // The render pipeline will ensure target setup and clearing happens in an performance manner.
    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        // Setup code here
    }

    protected override void Execute(CustomPassContext ctx)
    {
        var overrideSelection = ctx.hdCamera.camera.GetComponentInParent<SamplerOverrideSelection>();
        var materialOverrides = new MaterialSamplerOverride()
        {
            flags = SamplerOverrideFlags.None
        };

        if (overrideSelection != null)
        {
            materialOverrides.SetFlag(SamplerOverrideFlags.MipBias, overrideSelection.OverrideMipBias);
            materialOverrides.mipBias = overrideSelection.MipBiasOverride;

            materialOverrides.SetFlag(SamplerOverrideFlags.FilterMode, overrideSelection.OverrideFilter);
            materialOverrides.filterMode = overrideSelection.FilterOverride;
        }

        ctx.cmd.SetMaterialSamplerOverride(materialOverrides);
    }

    protected override void Cleanup()
    {
        // Cleanup code
    }
}
