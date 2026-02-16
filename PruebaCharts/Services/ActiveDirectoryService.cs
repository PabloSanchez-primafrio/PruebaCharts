using System.DirectoryServices.AccountManagement;
using System.Security.Principal;

namespace PruebaCharts.Services;

/// <summary>
/// Servicio para obtener información del usuario actual desde Active Directory.
/// Replica la lógica de QueryEx para validar permisos por Usuario, Grupos y OU.
/// </summary>
public class ActiveDirectoryService
{
    private static ActiveDirectoryService? _instance;
    private static readonly object _lock = new();

    private string? _currentUser;
    private List<string>? _userGroups;
    private string? _userOU;
    private bool? _isAdmin;

    public static ActiveDirectoryService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new ActiveDirectoryService();
                }
            }
            return _instance;
        }
    }

    private ActiveDirectoryService()
    {
        Initialize();
    }

    private void Initialize()
    {
        try
        {
            _currentUser = Environment.UserName.ToUpper();

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[AD] Usuario actual: {_currentUser}");
#endif

            LoadActiveDirectoryInfo();
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[AD] Error inicializando: {ex.Message}");
#endif
            _currentUser = Environment.UserName.ToUpper();
            _userGroups = new List<string>();
            _userOU = string.Empty;
        }
    }

    private void LoadActiveDirectoryInfo()
    {
        _userGroups = new List<string>();
        _userOU = string.Empty;

        try
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();

            var groupClaims = identity.Claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.GroupSid ||
                           c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/groupsid")
                .ToList();

            foreach (var claim in groupClaims)
            {
                try
                {
                    var sid = new System.Security.Principal.SecurityIdentifier(claim.Value);
                    var groupName = sid.Translate(typeof(System.Security.Principal.NTAccount)).Value;

                    if (groupName.Contains('\\'))
                    {
                        groupName = groupName.Split('\\').Last();
                    }

                    if (!_userGroups.Contains(groupName, StringComparer.OrdinalIgnoreCase))
                    {
                        _userGroups.Add(groupName);
                    }
                }
                catch
                {
                }
            }

            if (_userGroups.Count == 0 && identity.Groups != null)
            {
                foreach (var group in identity.Groups)
                {
                    try
                    {
                        var groupName = group.Translate(typeof(System.Security.Principal.NTAccount)).Value;
                        if (groupName.Contains('\\'))
                        {
                            groupName = groupName.Split('\\').Last();
                        }
                        if (!_userGroups.Contains(groupName, StringComparer.OrdinalIgnoreCase))
                        {
                            _userGroups.Add(groupName);
                        }
                    }
                    catch
                    {
                    }
                }
            }

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[AD-Claims] Grupos encontrados: {_userGroups.Count}");
            foreach (var g in _userGroups.Take(20))
                System.Diagnostics.Debug.WriteLine($"[AD]   - {g}");
#endif
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[AD] Error con Claims/WindowsIdentity: {ex.Message}");
#endif
        }

        try
        {
            using var context = new PrincipalContext(ContextType.Domain);
            using var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, _currentUser);

            if (user != null && !string.IsNullOrEmpty(user.DistinguishedName))
            {
                var parts = user.DistinguishedName.Split(',');
                if (parts.Length > 1)
                {
                    _userOU = string.Join(",", parts.Skip(1));
                }

#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[AD-Principal] OU: {_userOU}");
#endif
            }
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[AD] Error obteniendo OU (no crítico): {ex.Message}");
#endif
        }
    }

    public string CurrentUser => _currentUser ?? Environment.UserName.ToUpper();

    public List<string> UserGroups => _userGroups ?? new List<string>();

    public string UserOU => _userOU ?? string.Empty;

    public bool IsAdmin
    {
        get
        {
            if (_isAdmin.HasValue)
                return _isAdmin.Value;

            _isAdmin = CheckIsAdmin();
            return _isAdmin.Value;
        }
    }

    private bool CheckIsAdmin()
    {
        var adminUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "JOSECONESA",
            "NATALIAPEREZ",
            "JOSEMANUEL",
        };

        if (adminUsers.Contains(CurrentUser))
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[AD] Usuario {CurrentUser} es admin por nombre");
#endif
            return true;
        }

        var adminGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Informatica",
            "Gerencia",
            "Domain Admins",
            "Administradores",
        };

        foreach (var group in UserGroups)
        {
            if (adminGroups.Contains(group))
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[AD] Usuario {CurrentUser} es admin por grupo: {group}");
#endif
                return true;
            }
        }

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[AD] Usuario {CurrentUser} NO es admin");
#endif
        return false;
    }

    public bool TieneAcceso(string? permiso, string? permisoGrupos, string? permisoOU)
    {
        if (IsAdmin)
            return true;

        if (string.IsNullOrWhiteSpace(permiso) &&
            string.IsNullOrWhiteSpace(permisoGrupos) &&
            string.IsNullOrWhiteSpace(permisoOU))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(permiso))
        {
            var usuariosPermitidos = permiso.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var usuarioPermitido in usuariosPermitidos)
            {
                if (usuarioPermitido.Trim().Equals(CurrentUser, StringComparison.OrdinalIgnoreCase))
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[AD] Acceso permitido por usuario: {CurrentUser} en '{permiso}'");
#endif
                    return true;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(permisoGrupos))
        {
            var gruposPermitidos = permisoGrupos.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var grupoUsuario in UserGroups)
            {
                foreach (var grupoPermitido in gruposPermitidos)
                {
                    if (grupoUsuario.Equals(grupoPermitido.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[AD] Acceso permitido por grupo: {grupoUsuario}");
#endif
                        return true;
                    }
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(permisoOU) && !string.IsNullOrWhiteSpace(UserOU))
        {
            var ousPermitidas = permisoOU.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var ouPermitida in ousPermitidas)
            {
                if (UserOU.Contains(ouPermitida.Trim(), StringComparison.OrdinalIgnoreCase))
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[AD] Acceso permitido por OU: {UserOU} contiene '{ouPermitida}'");
#endif
                    return true;
                }
            }
        }

#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[AD] Acceso DENEGADO para usuario {CurrentUser}");
#endif
        return false;
    }

    public void Refresh()
    {
        _isAdmin = null;
        Initialize();
    }
}
