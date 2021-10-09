using System;
using System.Runtime.InteropServices;

namespace LibCap {
    public class CapError {
        public readonly ErrorTypes Type;
        public readonly string Msg;

        public enum ErrorTypes
        {
            FileNotFound,
            FileIsInvalid,
        }
        
        public CapError(ErrorTypes type, string msg) {
            this.Msg = msg;
            this.Type = type;
        }
    }
    
    public struct CapResult<T> {
        private readonly bool _isOk;
        private CapError _error;
        private T _value;

        public bool IsOk => _isOk;
        public T OkValue() {
            if (_isOk)
                return _value;
            
            throw new InvalidOperationException("You can't call OkValue() if CapResult is an error.");
        }
        
        public CapError ErrValue() {
            if (!_isOk)
                return _error;
            
            throw new InvalidOperationException("You can't call ErrValue() if CapResult is ok.");
        }
    
        private CapResult(T value) : this() {
            this._isOk = true;
            this._value = value;
        }
        
        private CapResult(CapError error) : this() {
            this._isOk = false;
            this._error = error;
        }
        
        public static CapResult<T> Ok(T value) {
            return new CapResult<T>(value);
        }
        
        public static CapResult<T> Err(CapError error) {
            return new CapResult<T>(error);
        }
    }
    
    public struct CapErrOption {
        private readonly bool _hasSomeError;
        private CapError _value;

        public bool HasSomeError => _hasSomeError;

        public CapError ErrValue() {
            if (_hasSomeError)
                return _value;
            
            throw new InvalidOperationException("You can't call ErrValue() if CapErrOption has no error.");
        }
        
        private CapErrOption(CapError value, bool isSome) {
            this._hasSomeError = isSome;
            this._value = value;
        }
        
        public static CapErrOption SomeErr(CapError value) {
            return new CapErrOption(value, true);
        }
        
        public static CapErrOption NoErr() {
            return new CapErrOption(null, false);
        }
    }
}