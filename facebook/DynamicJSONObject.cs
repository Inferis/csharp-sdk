using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;

namespace Facebook {
    public class DynamicJSONObject : DynamicObject {
        private JSONObject source;

        public DynamicJSONObject(JSONObject source)
        {
            this.source = source;
        }

        public override System.Collections.Generic.IEnumerable<string> GetDynamicMemberNames()
        {
            if (source.IsDictionary) {
                return source.Dictionary.Keys;
            }

            return new string[] { };
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (source.IsDictionary) {
                JSONObject jsonresult;
                if (source.Dictionary.TryGetValue(binder.Name, out jsonresult)) {
                    result = ToResult(jsonresult);
                    return true;
                }
            }

            result = null;
            return false;
        }

        private object ToResult(JSONObject json)
        {
            if (json.IsInteger)
                return json.Integer;
            else if (json.IsBoolean)
                return json.Boolean;
            else if (json.IsString)
                return json.String;
            else if (json.IsArray)
                return new DynamicJSONObject(json);
            else if (json.IsDictionary)
                return new DynamicJSONObject(json);
            return json;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if (indexes.Length == 1) {
                var converter = TypeDescriptor.GetConverter(indexes[0]);
                if (converter != null) {
                    if (source.IsArray && converter.CanConvertTo(typeof(int))) {
                        result = ToResult(source.Array[(int)converter.ConvertTo(indexes[0], typeof(int))]);
                        return true;
                    }
                    else if (source.IsDictionary && converter.CanConvertTo(typeof(string))) {
                        JSONObject jsonresult;
                        if (source.Dictionary.TryGetValue((string) converter.ConvertTo(indexes[0], typeof (string)), out jsonresult)) {
                            result = ToResult(jsonresult);
                            return true;
                        }
                    }
                }
            }

            result = null;
            return false;
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            result = null;
            if (binder.Type == typeof(string)) {
                if (source.IsString)
                    result = source.String;
                else
                    result = source.ToString();
                return true;
            }

            if (source.IsInteger) {
                if (binder.Type == typeof(int)) {
                    result = source.Integer;
                    return true;
                }
                else if (binder.Type == typeof(float)) {
                    result = Convert.ToSingle(source.Integer);
                    return true;
                }
                else if (binder.Type == typeof(double)) {
                    result = Convert.ToDouble(source.Integer);
                    return true;
                }
                else if (binder.Type == typeof(long)) {
                    result = Convert.ToInt64(source.Integer);
                    return true;
                }
                else if (binder.Type == typeof(bool)) {
                    result = Convert.ToBoolean(source.Integer);
                    return true;
                }

                return false;
            }

            if (source.IsBoolean) {
                if (binder.Type == typeof(bool)) {
                    result = source.Boolean;
                    return true;
                }
                else if (binder.Type == typeof(int)) {
                    result = Convert.ToBoolean(source.Boolean);
                    return true;
                }
                else if (binder.Type == typeof(float)) {
                    result = Convert.ToSingle(source.Boolean);
                    return true;
                }
                else if (binder.Type == typeof(double)) {
                    result = Convert.ToDouble(source.Boolean);
                    return true;
                }
                else if (binder.Type == typeof(long)) {
                    result = Convert.ToInt64(source.Boolean);
                    return true;
                }

                return false;
            }

            if (source.IsArray) {
                if (binder.Type.IsArray) {
                    result = source.Array.Select(x => new DynamicJSONObject(x)).ToArray();
                    return true;
                }

                if (binder.Type.IsAssignableFrom(typeof(IEnumerable))) {
                    result = source.Array.Select(x => new DynamicJSONObject(x)).ToArray();
                    return true;
                }

                if (binder.Type.IsGenericType) {
                    var gtype = binder.Type.GetGenericTypeDefinition();
                    if (gtype.IsAssignableFrom(typeof(IEnumerable<>))) {
                        result = source.Array.Select(x => new DynamicJSONObject(x)).ToArray();
                        return true;
                    }
                }

                return false;
            }

            if (source.IsDictionary) {
                if (binder.Type.IsAssignableFrom(typeof(IDictionary))) {
                    result = source.Dictionary.ToDictionary(kv => kv.Key, kv => new DynamicJSONObject(kv.Value));
                    return true;
                }

                if (binder.Type.IsGenericType) {
                    var gtype = binder.Type.GetGenericTypeDefinition();
                    if (gtype.IsAssignableFrom(typeof(IDictionary<,>)) && binder.Type.GetGenericArguments()[0] == typeof(string)) {
                        result = source.Dictionary.ToDictionary(kv => kv.Key, kv => new DynamicJSONObject(kv.Value));
                        return true;
                    }
                }

                return false;
            }

            return false;
        }
    }
}