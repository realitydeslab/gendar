// OSC Jack - Open Sound Control plugin for Unity
// https://github.com/keijiro/OscJack

using UnityEngine;
using System;
using System.Reflection;


/// <summary>
/// Add Range To Property Sender
/// </summary>

namespace OscJack
{
    [AddComponentMenu("OSC/Property Sender Modified")]
    public sealed class OscPropertySenderModified : MonoBehaviour
    {
        #region Editable fields

        [SerializeField]
        public OscConnection _connection = null;
        [SerializeField]
        public string _oscAddress = "/unity";
        [SerializeField]
        public Component _dataSource = null;
        [SerializeField]
        public string _propertyName = "";
        [SerializeField]
        public bool _keepSending = false;
        [SerializeField]
        public float _timeInterval = 0;
        float _lastSendTime = 0;

        [SerializeField] bool _needRemapping = true;
        public bool _needClamp = true;
        public Vector2 _srcRange = new Vector2(0, 1);
        public Vector2 _dstRange = new Vector2(0, 1);

        public bool successfullySend = false;

        #endregion

        #region Internal members

        OscClient _client;
        PropertyInfo _propertyInfo;

        void UpdateSettings()
        {
            if (_connection != null)
                _client = OscMaster.GetSharedClient(_connection.host, _connection.port);
            else
                _client = null;

            if (_dataSource != null && !string.IsNullOrEmpty(_propertyName))
                _propertyInfo = _dataSource.GetType().GetProperty(_propertyName);
            else
                _propertyInfo = null;
        }

        #endregion

        #region Remap Function
        static float Remap(float v, float src_min, float src_max, float dst_min, float dst_max, bool clamp = true)
        {
            if (clamp)
                v = Mathf.Clamp(v, src_min, src_max);

            if (src_min == src_max)
                return 0;

            return (v - src_min) / (src_max - src_min) * (dst_max - dst_min) + dst_min;
        }
        #endregion


        #region MonoBehaviour implementation

        void Start()
        {
            UpdateSettings();
        }

        void OnValidate()
        {
            if (Application.isPlaying) UpdateSettings();
        }

        void Update()
        {
            if (_client == null || _propertyInfo == null) return;

            var type = _propertyInfo.PropertyType;
            var value = _propertyInfo.GetValue(_dataSource, null); // boxing!!

            if (type == typeof(int)) Send((int)value);
            else if (type == typeof(float)) Send((float)value);
            else if (type == typeof(string)) Send((string)value);
            else if (type == typeof(Vector2)) Send((Vector2)value);
            else if (type == typeof(Vector3)) Send((Vector3)value);
            else if (type == typeof(Vector4)) Send((Vector4)value);
            else if (type == typeof(Vector2Int)) Send((Vector2Int)value);
            else if (type == typeof(Vector3Int)) Send((Vector3Int)value);
        }

        #endregion

        #region Sender methods

        int _intValue = Int32.MaxValue;

        public void Send(int data)
        {
            if (_needRemapping) data = Mathf.FloorToInt(Remap(data, _srcRange.x, _srcRange.y, _dstRange.x, _dstRange.y, _needClamp));
            
            if (!_keepSending && data == _intValue) return;

            if (_keepSending && Time.time - _lastSendTime < _timeInterval) return;

            try
            {
                _client.Send(_oscAddress, data);
                successfullySend = true;
                _lastSendTime = Time.time;
            }
            catch
            {
                successfullySend = false;
            }

            _intValue = data;
        }

        public float _floatValue = Single.MaxValue;
        public float _originalFloatValue = Single.MaxValue;

        public void Send(float data)
        {
            _originalFloatValue = data;
            if (_needRemapping) data = Remap(data, _srcRange.x, _srcRange.y, _dstRange.x, _dstRange.y, _needClamp);

            if (!_keepSending && data == _floatValue) return;

            if (_keepSending && Time.time - _lastSendTime < _timeInterval) return;

            try
            {
                _client.Send(_oscAddress, data);
                successfullySend = true;
                _lastSendTime = Time.time;
            }
            catch {
                successfullySend = false;
            }
            _floatValue = data;
        }

        Vector2 _vector2Value = new Vector2(Single.MaxValue, 0);

        public void Send(Vector2 data)
        {
            if (_needRemapping)
            {
                data.x = Remap(data.x, _srcRange.x, _srcRange.y, _dstRange.x, _dstRange.y, _needClamp);
                data.y = Remap(data.y, _srcRange.x, _srcRange.y, _dstRange.x, _dstRange.y, _needClamp);
            }

            if (!_keepSending && data == _vector2Value) return;

            if (_keepSending && Time.time - _lastSendTime < _timeInterval) return;

            try
            {
                _client.Send(_oscAddress, data.x, data.y);
                successfullySend = true;
                _lastSendTime = Time.time;
            }
            catch
            {
                successfullySend = false;
            }

            _vector2Value = data;
        }

        Vector3 _vector3Value = new Vector3(Single.MaxValue, 0, 0);

        public void Send(Vector3 data)
        {
            if (_needRemapping)
            {
                data.x = Remap(data.x, _srcRange.x, _srcRange.y, _dstRange.x, _dstRange.y, _needClamp);
                data.y = Remap(data.y, _srcRange.x, _srcRange.y, _dstRange.x, _dstRange.y, _needClamp);
                data.z = Remap(data.z, _srcRange.x, _srcRange.y, _dstRange.x, _dstRange.y, _needClamp);
            }

            if (!_keepSending && data == _vector3Value) return;

            if (_keepSending && Time.time - _lastSendTime < _timeInterval) return;

            try
            {
                _client.Send(_oscAddress, data.x, data.y, data.z);
                successfullySend = true;
                _lastSendTime = Time.time;
            }
            catch
            {
                successfullySend = false;
            }
           
            _vector3Value = data;
        }

        Vector4 _vector4Value = new Vector4(Single.MaxValue, 0, 0, 0);

        public void Send(Vector4 data)
        {
            if (_needRemapping)
            {
                data.x = Remap(data.x, _srcRange.x, _srcRange.y, _dstRange.x, _dstRange.y, _needClamp);
                data.y = Remap(data.y, _srcRange.x, _srcRange.y, _dstRange.x, _dstRange.y, _needClamp);
                data.z = Remap(data.z, _srcRange.x, _srcRange.y, _dstRange.x, _dstRange.y, _needClamp);
                data.w = Remap(data.w, _srcRange.x, _srcRange.y, _dstRange.x, _dstRange.y, _needClamp);
            }

            if (!_keepSending && data == _vector4Value) return;

            if (_keepSending && Time.time - _lastSendTime < _timeInterval) return;

            try
            {
                _client.Send(_oscAddress, data.x, data.y, data.z, data.w);
                successfullySend = true;
                _lastSendTime = Time.time;
            }
            catch
            {
                successfullySend = false;
            }
            
            _vector4Value = data;
        }

        Vector2Int _vector2IntValue = new Vector2Int(Int32.MaxValue, 0);

        public void Send(Vector2Int data)
        {
            if (_needRemapping)
            {
                data.x = Mathf.FloorToInt(Remap(data.x, _srcRange.x, _srcRange.y, _dstRange.x, _dstRange.y, _needClamp));
                data.y = Mathf.FloorToInt(Remap(data.y, _srcRange.x, _srcRange.y, _dstRange.x, _dstRange.y, _needClamp));
            }

            if (!_keepSending && data == _vector2IntValue) return;

            if (_keepSending && Time.time - _lastSendTime < _timeInterval) return;

            try
            {
                _client.Send(_oscAddress, data.x, data.y);
                successfullySend = true;
                _lastSendTime = Time.time;
            }
            catch
            {
                successfullySend = false;
            }

            _vector2IntValue = data;
        }

        Vector3Int _vector3IntValue = new Vector3Int(Int32.MaxValue, 0, 0);

        public void Send(Vector3Int data)
        {
            if (_needRemapping)
            {
                data.x = Mathf.FloorToInt(Remap(data.x, _srcRange.x, _srcRange.y, _dstRange.x, _dstRange.y, _needClamp));
                data.y = Mathf.FloorToInt(Remap(data.y, _srcRange.x, _srcRange.y, _dstRange.x, _dstRange.y, _needClamp));
                data.z = Mathf.FloorToInt(Remap(data.z, _srcRange.x, _srcRange.y, _dstRange.x, _dstRange.y, _needClamp));
            }

            if (!_keepSending && data == _vector3IntValue) return;

            if (_keepSending && Time.time - _lastSendTime < _timeInterval) return;

            try
            {
                _client.Send(_oscAddress, data.x, data.y, data.z);
                successfullySend = true;
                _lastSendTime = Time.time;
            }
            catch
            {
                successfullySend = false;
            }

            _vector3IntValue = data;
        }

        string _stringValue = string.Empty;

        public void Send(string data)
        {
            if (!_keepSending && data == _stringValue) return;

            if (_keepSending && Time.time - _lastSendTime < _timeInterval) return;

            try
            {
                _client.Send(_oscAddress, data);
                successfullySend = true;
                _lastSendTime = Time.time;
            }
            catch
            {
                successfullySend = false;
            }

            _stringValue = data;
        }

        #endregion
    }
}