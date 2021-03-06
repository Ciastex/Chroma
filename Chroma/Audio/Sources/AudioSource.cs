﻿using System;
using System.Collections.Generic;
using Chroma.MemoryManagement;
using Chroma.Natives.SDL;

namespace Chroma.Audio.Sources
{
    public abstract class AudioSource : DisposableResource
    {
        public delegate void AudioStreamDelegate(Span<byte> audioBufferData, AudioFormat format);

        protected IntPtr Handle { get; set; }

        internal unsafe SDL2_nmix.NMIX_Source* Source
            => (SDL2_nmix.NMIX_Source*)Handle.ToPointer();

        public virtual PlaybackStatus Status { get; set; }

        public bool IsPlaying => SDL2_nmix.NMIX_IsPlaying(Handle);

        public float Panning
        {
            get => SDL2_nmix.NMIX_GetPan(Handle);
            set
            {
                var pan = value;

                if (pan < -1.0f)
                    pan = 1.0f;

                if (pan > 1.0f)
                    pan = 1.0f;

                SDL2_nmix.NMIX_SetPan(Handle, pan);
            }
        }

        public float Volume
        {
            get => SDL2_nmix.NMIX_GetGain(Handle);

            set
            {
                var vol = value;

                if (vol < 0f)
                    vol = 0f;

                if (vol > 2f)
                    vol = 2f;

                SDL2_nmix.NMIX_SetGain(Handle, vol);
            }
        }

        public AudioFormat Format
        {
            get
            {
                unsafe
                {
                    return AudioFormat.FromSdlFormat(
                        Source->format
                    );
                }
            }
        }

        public byte ChannelCount
        {
            get
            {
                unsafe
                {
                    return Source->channels;
                }
            }
        }
        
        public Span<byte> InBuffer
        {
            get
            {
                unsafe
                {
                    return new Span<byte>(Source->in_buffer.ToPointer(), Source->in_buffer_size);
                }
            }
        }

        public Span<byte> OutBuffer
        {
            get
            {
                unsafe
                {
                    return new Span<byte>(Source->out_buffer.ToPointer(), Source->out_buffer_size);
                }
            }
        }

        public List<AudioStreamDelegate> Filters { get; } = new();

        public virtual void Play()
        {
            EnsureHandleValid();
            SDL2_nmix.NMIX_Play(Handle);
        }

        public virtual void Pause()
        {
            EnsureHandleValid();
            SDL2_nmix.NMIX_Pause(Handle);
        }

        public virtual void Stop()
            => throw new NotSupportedException("This audio source does not support stopping.");

        protected void EnsureHandleValid()
        {
            if (Handle == IntPtr.Zero)
                throw new InvalidOperationException("Audio source handle is not valid.");
        }

        protected override void FreeNativeResources()
        {
            if (Handle != IntPtr.Zero)
            {
                SDL2_nmix.NMIX_FreeSource(Handle);
                Handle = IntPtr.Zero;
            }
        }
    }
}