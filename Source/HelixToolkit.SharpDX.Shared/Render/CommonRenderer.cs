﻿/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

//#define OLD

using SharpDX.Direct3D11;
using System.Collections.Generic;
#if NETFX_CORE
namespace HelixToolkit.UWP.Render
#else
namespace HelixToolkit.Wpf.SharpDX.Render
#endif
{
    using Core;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// 
    /// </summary>
    public class CommonRenderer : DisposeObject, IRenderer
    {
        private readonly Stack<IEnumerator<IRenderable>> stackCache1 = new Stack<IEnumerator<IRenderable>>(20);
        private readonly Stack<IEnumerator<IRenderable>> stackCache2 = new Stack<IEnumerator<IRenderable>>(20);

        public DeviceContextProxy ImmediateContext { private set; get; }     

        public CommonRenderer(Device device)
        {
            ImmediateContext = Collect(new DeviceContextProxy(device.ImmediateContext));
        }
        /// <summary>
        /// Updates the scene graph.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="renderables">The renderables.</param>
        /// <param name="parameter">The parameter.</param>
        /// <returns></returns>
        public IEnumerable<IRenderable> UpdateSceneGraph(IRenderContext context, IEnumerable<IRenderable> renderables)
        {
            return renderables.PreorderDFT((x) =>
                    {
                        x.Update(context);
                        return x.IsRenderable && !(x is ILight3D);
                    }, stackCache1);
        }
        /// <summary>
        /// Updates the global variables. Such as light buffer and global transformation buffer
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="renderables">The renderables.</param>
        /// <param name="parameter">The parameter.</param>
        public void UpdateGlobalVariables(IRenderContext context, IEnumerable<IRenderable> renderables, ref RenderParameter parameter)
        {
            context.LightScene.LightModels.ResetLightCount();
            foreach (IRenderable e in renderables.Take(Constants.MaxLights)
                .PreorderDFT((x)=> x is ILight3D && x.IsRenderable, stackCache2).Take(Constants.MaxLights))
            {
                e.Render(context, ImmediateContext);
            }
            context.UpdatePerFrameData();
        }

        /// <summary>
        /// Renders the scene.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="renderables">The renderables.</param>
        /// <param name="parameter">The parameter.</param>
        public void RenderScene(IRenderContext context, DeviceContextProxy deviceContext, IEnumerable<IRenderCore> renderables, ref RenderParameter parameter)
        {
            SetRenderTargets(deviceContext, ref parameter);
            foreach (var renderable in renderables)
            {
                renderable.Render(context, deviceContext);
            }
        }
        /// <summary>
        /// Updates the no render parallel. <see cref="IRenderer.UpdateNotRenderParallel(IEnumerable{IRenderable})"/>
        /// </summary>
        /// <param name="renderables">The renderables.</param>
        /// <returns></returns>
        public void UpdateNotRenderParallel(IEnumerable<IRenderable> renderables)
        {
            foreach(var model in renderables)
            {
                model.UpdateNotRender();
            }
        }
        /// <summary>
        /// Sets the render targets.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="parameter">The parameter.</param>
        private void SetRenderTargets(DeviceContext context, ref RenderParameter parameter)
        {
            context.OutputMerger.SetTargets(parameter.depthStencil, parameter.target);
            context.Rasterizer.SetViewport(parameter.ViewportRegion);
            context.Rasterizer.SetScissorRectangle(parameter.ScissorRegion.Left, parameter.ScissorRegion.Top, 
                parameter.ScissorRegion.Right, parameter.ScissorRegion.Bottom);
        }

        /// <summary>
        /// Render2s the d.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="renderables">The renderables.</param>
        /// <param name="parameter">The parameter.</param>
        public void Render2D(IRenderContext2D context, IEnumerable<IRenderable2D> renderables, ref RenderParameter2D parameter)
        {
            foreach (var e in renderables)
            {
                e.Render(context);
            }
        }
    }

}
