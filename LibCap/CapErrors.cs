using System;
using System.Runtime.InteropServices;

namespace LibCap {
    public class CapError {
        public readonly ErrorTypes Type;
        public readonly string Msg;
        public bool HasError { get {return Type != ErrorTypes.NoErrors;} }
        public enum ErrorTypes
        {
            NoErrors,
            FileNotFound,
            FileIsInvalid,
        }
        
        public CapError(ErrorTypes type, string msg) {
            this.Msg = msg;
            this.Type = type;
        }
        
        public static CapError NoError() {
            return new CapError(ErrorTypes.NoErrors, null);
        }
    }
}