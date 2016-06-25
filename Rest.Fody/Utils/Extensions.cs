﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Rest.Fody.Weaving
{
    public static class Utils
    {
        #region Misc
        public static void Emit(this Mono.Cecil.Cil.MethodBody body, Action<ILProcessor> il)
        {
            il(body.GetILProcessor());
        }

        public static void EmitToBeginning(this Mono.Cecil.Cil.MethodBody body, params Instruction[] il)
        {
            if (il.Length == 0) return;
            var proc = body.GetILProcessor();

            if (body.Instructions.Count == 0)
            {
                foreach (var i in il)
                    proc.Append(i);
            }
            else
            {
                proc.InsertBefore(body.Instructions[0], il[0]);

                if (il.Length == 1) return;
                for (int i = 1; i < il.Length; i++)
                {
                    proc.InsertAfter(body.Instructions[i - 1], il[i]);
                }
            }
        }

        public static void EmitMany(this Mono.Cecil.Cil.MethodBody body, IEnumerable<Instruction> instructions)
        {
            body.Emit(il =>
            {
                foreach (Instruction i in instructions)
                    il.Append(i);
            });
        }


        public static bool Is<T>(this TypeReference typeRef, bool acceptDerivedTypes = false)
        {
            return Is(typeRef, typeof(T), acceptDerivedTypes);
        }

        public static bool Is(this TypeReference typeRef, Type t, bool acceptDerivedTypes = false)
        {
            TypeDefinition def;
            return acceptDerivedTypes
                ? (def = typeRef.Resolve()).FullName == t.FullName || (def.BaseType != null && def.BaseType.Is(t))
                : typeRef.FullName == t.FullName;
        }

        public static Type AsType(this TypeReference typeRef)
        {
            return Type.GetType(Assembly.CreateQualifiedName(typeRef.Module.Assembly.FullName, typeRef.FullName));
        }
        #endregion

        #region Generic
        public static GenericInstanceMethod MakeGenericMethod(this MethodReference method, params TypeReference[] genericArguments)
        {
            var result = new GenericInstanceMethod(method);
            foreach (var argument in genericArguments)
                result.GenericArguments.Add(argument);
            return result;
        }

        public static GenericInstanceType MakeGenericType(this TypeReference type, params TypeReference[] genericArguments)
        {
            var result = new GenericInstanceType(type);
            foreach (var argument in genericArguments)
                result.GenericArguments.Add(argument);
            return result;
        }
        #endregion

        #region Import
        public static TypeReference ImportType<T>(this ModuleDefinition module)
        {
            return module.Import(typeof(T));
        }

        public static MethodReference ImportMethod<T>(this ModuleDefinition module, string name, params Type[] paramTypes)
        {
            return module.Import(typeof(T).GetMethod(name, paramTypes));
        }

        public static MethodReference ImportContinueWith(this ModuleDefinition module, TypeReference target)
        {
            return (MethodReference)typeof(Utils)
                .GetMethod("ImportContinueWith", new Type[] { typeof(ModuleDefinition) })
                .MakeGenericMethod(target.AsType())
                .Invoke(null, new object[] { module });
        }

        public static MethodReference ImportContinueWith(this ModuleDefinition module, Type target1, Type target2)
        {
            return module.Import((from m in target1.GetMethods()
                                  where m.Name == "ContinueWith"
                                  let p = m.GetParameters()
                                  where p.Length == 1 && p[0].ParameterType.Name == "Func`2"
                                  select m.MakeGenericMethod(target2)).First());
        }

        public static MethodReference ImportContinueWith<T>(this ModuleDefinition module)
        {
            return module.Import((from m in typeof(Task<HttpResponseMessage>).GetMethods()
                                  where m.Name == "ContinueWith"
                                  let p = m.GetParameters()
                                  where p.Length == 1 && p[0].ParameterType.Name == "Func`2"
                                  select m.MakeGenericMethod(typeof(T))).First());
        }

        public static FieldReference ImportField<T, TField>(ModuleDefinition module, Expression<Func<T, TField>> ex)
        {
            MemberExpression dp = ex.Body as MemberExpression;
            return module.Import(typeof(T).GetField(dp.Member.Name));
        }

        public static MethodReference ImportGetter<T, TProp>(this ModuleDefinition module, Expression<Func<T, TProp>> ex)
        {
            MemberExpression dp = ex.Body as MemberExpression;
            return module.Import(typeof(T).GetProperty(dp.Member.Name).GetMethod);
        }

        public static MethodReference ImportSetter<T, TProp>(this ModuleDefinition module, Expression<Func<T, TProp>> ex)
        {
            MemberExpression dp = ex.Body as MemberExpression;
            return module.Import(typeof(T).GetProperty(dp.Member.Name).SetMethod);
        }

        public static MethodReference ImportCtor<T>(this ModuleDefinition module, params Type[] paramTypes)
        {
            return module.Import(typeof(T).GetConstructor(paramTypes));
        }
        #endregion


        #region Custom Attributes
        public static CustomAttribute GetAttr<TAttr>(this MethodDefinition def) where TAttr : Attribute
        {
            return def.CustomAttributes.FirstOrDefault(x => x.AttributeType.Is<TAttr>(true));
        }

        public static CustomAttribute GetAttr<TAttr>(this TypeDefinition def) where TAttr : Attribute
        {
            return def.CustomAttributes.FirstOrDefault(x => x.AttributeType.Is<TAttr>(true));
        }

        public static CustomAttribute GetAttr<TAttr>(this FieldDefinition def) where TAttr : Attribute
        {
            return def.CustomAttributes.FirstOrDefault(x => x.AttributeType.Is<TAttr>(true));
        }

        public static CustomAttribute GetAttr<TAttr>(this PropertyDefinition def) where TAttr : Attribute
        {
            return def.CustomAttributes.FirstOrDefault(x => x.AttributeType.Is<TAttr>(true));
        }

        public static CustomAttribute GetAttr<TAttr>(this ParameterDefinition def) where TAttr : Attribute
        {
            return def.CustomAttributes.FirstOrDefault(x => x.AttributeType.Is<TAttr>(true));
        }

        public static IEnumerable<CustomAttribute> GetAttrs<TAttr>(this MethodDefinition def) where TAttr : Attribute
        {
            return def.CustomAttributes.Where(x => x.AttributeType.Is<TAttr>(true));
        }

        public static IEnumerable<CustomAttribute> GetAttrs<TAttr>(this TypeDefinition def) where TAttr : Attribute
        {
            return def.CustomAttributes.Where(x => x.AttributeType.Is<TAttr>(true));
        }

        public static IEnumerable<CustomAttribute> GetAttrs<TAttr>(this FieldDefinition def) where TAttr : Attribute
        {
            return def.CustomAttributes.Where(x => x.AttributeType.Is<TAttr>(true));
        }

        public static IEnumerable<CustomAttribute> GetAttrs<TAttr>(this PropertyDefinition def) where TAttr : Attribute
        {
            return def.CustomAttributes.Where(x => x.AttributeType.Is<TAttr>(true));
        }

        public static IEnumerable<CustomAttribute> GetAttrs<TAttr>(this ParameterDefinition def) where TAttr : Attribute
        {
            return def.CustomAttributes.Where(x => x.AttributeType.Is<TAttr>(true));
        }
        #endregion
    }
}