namespace BovineLabs.Core.ConfigVars
{
    using System;
    using Unity.Burst;
    using Unity.Collections;

    /// <summary>
    /// Debugging only; T must be a FixedString type.
    /// </summary>
    internal class ConfigVarSharedStaticStringContainer<T> : IConfigVarContainer<T>
        where T : unmanaged
    {
        private readonly SharedStatic<T> _field;

        public ConfigVarSharedStaticStringContainer(SharedStatic<T> field)
        {
            _field = field;
        }

        T IConfigVarContainer<T>.Value
        {
            get => _field.Data;
            set => _field.Data = value;
        }

        string IConfigVarContainer.StringValue
        {
            get => _field switch
            {
                SharedStatic<FixedString32Bytes> s32 => s32.Data.ToString(),
                SharedStatic<FixedString64Bytes> s64 => s64.Data.ToString(),
                SharedStatic<FixedString128Bytes> s128 => s128.Data.ToString(),
                SharedStatic<FixedString512Bytes> s512 => s512.Data.ToString(),
                SharedStatic<FixedString4096Bytes> s4096 => s4096.Data.ToString(),
                _ => throw new InvalidOperationException("String config var must be a FixedString type"),
            };

            set
            {
                switch (_field)
                {
                    case SharedStatic<FixedString32Bytes> s32:
                        s32.Data = new FixedString32Bytes(value);

                        break;
                    case SharedStatic<FixedString64Bytes> s64:
                        s64.Data = new FixedString64Bytes(value);

                        break;
                    case SharedStatic<FixedString128Bytes> s128:
                        s128.Data = new FixedString128Bytes(value);

                        break;
                    case SharedStatic<FixedString512Bytes> s512:
                        s512.Data = new FixedString512Bytes(value);

                        break;
                    case SharedStatic<FixedString4096Bytes> s4096:
                        s4096.Data = new FixedString4096Bytes(value);

                        break;
                    default:
                        throw new InvalidOperationException("String config var must be a FixedString type");
                }
            }
        }

        public Type Type
        {
            get
            {
                return _field switch
                {
                    SharedStatic<FixedString32Bytes> => typeof(FixedString32Bytes),
                    SharedStatic<FixedString64Bytes> => typeof(FixedString64Bytes),
                    SharedStatic<FixedString128Bytes> => typeof(FixedString128Bytes),
                    SharedStatic<FixedString512Bytes> => typeof(FixedString512Bytes),
                    SharedStatic<FixedString4096Bytes> => typeof(FixedString4096Bytes),
                    _ => throw new InvalidOperationException("String config var must be a FixedString type"),
                };
            }
        }
    }
}
