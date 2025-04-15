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
                throw new FileNotFoundException($"El archivo {jsonPath} no fue encontrado.");

            var json = File.ReadAllText(jsonPath);
            var serviceDefinitions = JsonSerializer.Deserialize<List<ServiceDefinition>>(json);

            foreach (var def in serviceDefinitions)
            {
                var interfaceType = FindType(def.Interface);
                var implementationType = FindType(def.Implementation);

                if (interfaceType == null || implementationType == null)
                    throw new Exception($"No se pudo cargar tipo: {def.Interface} o {def.Implementation}");

                switch (def.Lifetime.ToLower())
                {
                    case "singleton":
                        services.AddSingleton(interfaceType, implementationType);
                        break;
                    case "scoped":
                        services.AddScoped(interfaceType, implementationType);
                        break;
                    case "transient":
                        services.AddTransient(interfaceType, implementationType);
                        break;
                    default:
                        throw new Exception($"Lifetime no soportado: {def.Lifetime}");
                }
            }
        }

        private static Type? FindType(string fullTypeName)
        {
            Console.WriteLine($"[INFO] Buscando tipo: {fullTypeName}");

            // Buscar en los ensamblados ya cargados
            var type = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.FullName == fullTypeName);

            Console.WriteLine(type != null
                ? $"[OK] Tipo encontrado en ensamblados cargados: {type.FullName}"
                : $"[WARN] Tipo NO encontrado en ensamblados cargados. Intentando cargar manualmente...");

            if (type != null)
                return type;

            // Intentar cargar manualmente otros ensamblados del directorio
            var loadedAssemblyNames = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(a => a.GetName().FullName)
                .ToHashSet();

            foreach (var dll in Directory.GetFiles(AppContext.BaseDirectory, "*.dll"))
            {
                try
                {
                    var assemblyName = AssemblyName.GetAssemblyName(dll);
                    if (loadedAssemblyNames.Contains(assemblyName.FullName))
                        continue;

                    var assembly = Assembly.Load(assemblyName);
                    type = assembly.GetType(fullTypeName);

                    if (type != null)
                    {
                        Console.WriteLine($"[OK] Tipo encontrado después de cargar: {type.FullName}");
                        return type;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] No se pudo cargar el ensamblado {dll}: {ex.Message}");
                }
            }

            Console.WriteLine($"[FAIL] Tipo {fullTypeName} no encontrado en ningún ensamblado.");
            return null;
        }
    }
}
