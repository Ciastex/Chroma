﻿using System.Numerics;
using Chroma.Diagnostics;
using Chroma.Natives.SDL;

namespace Chroma.Graphics.Accelerated
{
    public abstract class ShaderBase : DisposableResource
    {
        internal uint ProgramHandle { get; set; }
        internal uint VertexShaderObjectHandle { get; set; }
        internal uint PixelShaderObjectHandle { get; set; }

        internal SDL_gpu.GPU_ShaderBlock Block;

        public void Activate()
        {
            if (ProgramHandle == 0)
            {
                Log.Warning($"Refusing to activate invalid shader.");
                return;
            }

            SDL_gpu.GPU_ActivateShaderProgram(ProgramHandle, ref Block);
        }

        public void SetUniform(string name, float value)
        {
            var loc = SDL_gpu.GPU_GetUniformLocation(ProgramHandle, name);

            if (loc == -1)
            {
                Log.Warning($"Float uniform '{name}' does not exist.");
                return;
            }

            SDL_gpu.GPU_SetUniformf(loc, value);
        }

        public void SetUniform(string name, int value)
        {
            var loc = SDL_gpu.GPU_GetUniformLocation(ProgramHandle, name);

            if (loc == -1)
            {
                Log.Warning($"Int uniform '{name}' does not exist.");
                return;
            }

            SDL_gpu.GPU_SetUniformi(loc, value);
        }

        public void SetUniform(string name, Vector2 value)
        {
            var loc = SDL_gpu.GPU_GetUniformLocation(ProgramHandle, name);

            if (loc == -1)
            {
                Log.Warning($"Vec2 uniform '{name}' does not exist.");
                return;
            }

            SDL_gpu.GPU_SetUniformfv(loc, 2, 1, new float[] { value.X, value.Y });
        }

        public void SetUniform(string name, Color value)
        {
            var loc = SDL_gpu.GPU_GetUniformLocation(ProgramHandle, name);

            if (loc == -1)
            {
                Log.Warning($"Vec4 uniform '{name}' does not exist.");
                return;
            }

            SDL_gpu.GPU_SetUniformfv(loc, 4, 1, value.AsOrderedArray());
        }
    }
}
