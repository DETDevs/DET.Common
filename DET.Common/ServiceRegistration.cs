using DET.Common.Model;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text.Json;

namespace DET.Common
{
    public static class ServiceRegistration
    {
        public static void AddConfigServices(this IServiceCollection services, string jsonPath = "services.json")
        {
            if (!File.Exists(jsonPath))
            {
                throw new FileNotFoundException("El archivo " + jsonPath + " no fue encontrado.");
            }

            string json = File.ReadAllText(jsonPath);
            List<ServiceDefinition> list = JsonSerializer.Deserialize<List<ServiceDefinition>>(json);
            foreach (ServiceDefinition item in list)
            {
                Type implementationType = FindType(item.Implementation);
                if (implementationType == null)
                {
                    throw new Exception("No se pudo cargar la implementación: " + item.Implementation);
                }

                Type interfaceType = null;
                if (!string.IsNullOrWhiteSpace(item.Interface))
                {
                    interfaceType = FindType(item.Interface);
                    if (interfaceType == null)
                    {
                        throw new Exception("No se pudo cargar la interfaz: " + item.Interface);
                    }
                }

                switch (item.Lifetime.ToLower())
                {
                    case "singleton":
                        if (interfaceType != null)
                            services.AddSingleton(interfaceType, implementationType);
                        else
                            services.AddSingleton(implementationType);
                        break;

                    case "scoped":
                        if (interfaceType != null)
                            services.AddScoped(interfaceType, implementationType);
                        else
                            services.AddScoped(implementationType);
                        break;

                    case "transient":
                        if (interfaceType != null)
                            services.AddTransient(interfaceType, implementationType);
                        else
                            services.AddTransient(implementationType);
                        break;

                    default:
                        throw new Exception("Lifetime no soportado: " + item.Lifetime);
                }
            }
        }


        private static Type? FindType(string fullTypeName)
        {
            Console.WriteLine($"[INFO] Buscando tipo: {fullTypeName}");

            // 1. Cargar todos los ensamblados posibles antes de buscar
            var loadedAssemblyNames = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(a => a.GetName().FullName)
                .ToHashSet();

            foreach (var dll in Directory.GetFiles(AppContext.BaseDirectory, "*.dll"))
            {
                try
                {
                    var assemblyName = AssemblyName.GetAssemblyName(dll);
                    if (!loadedAssemblyNames.Contains(assemblyName.FullName))
                    {
                        Assembly.Load(assemblyName);
                        Console.WriteLine($"[INFO] Ensamblado cargado: {assemblyName.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] No se pudo cargar el ensamblado {dll}: {ex.Message}");
                }
            }

            // 2. Ahora que todos están cargados, buscar el tipo
            var type = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); } // Ignora ensamblados que no puedan leerse
                })
                .FirstOrDefault(t => t.FullName == fullTypeName);

            if (type != null)
            {
                Console.WriteLine($"[OK] Tipo encontrado: {type.FullName}");
                return type;
            }

            Console.WriteLine($"[FAIL] Tipo {fullTypeName} no encontrado.");
            return null;
        }

    }
}