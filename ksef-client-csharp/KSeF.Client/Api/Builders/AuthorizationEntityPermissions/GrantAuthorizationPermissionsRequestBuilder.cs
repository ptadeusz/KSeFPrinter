using KSeF.Client.Core.Models.Permissions.Authorizations;
using AuthorizationPermissionType = KSeF.Client.Core.Models.Permissions.Authorizations.AuthorizationPermissionType;

namespace KSeF.Client.Api.Builders.AuthorizationPermissions;

public static class GrantAuthorizationPermissionsRequestBuilder
{
    public static ISubjectStep Create() => GrantPermissionsRequestBuilderImpl.Create();

    public interface ISubjectStep
    {
        IPermissionsStep WithSubject(SubjectIdentifier subject);
    }

    public interface IPermissionsStep
    {
        IOptionalStep WithPermission(AuthorizationPermissionType permission);
    }

    public interface IOptionalStep
    {
        IOptionalStep WithDescription(string description);
        GrantAuthorizationPermissionsRequest Build();
    }

    private sealed class GrantPermissionsRequestBuilderImpl :
        ISubjectStep,
        IPermissionsStep,
        IOptionalStep
    {
        private SubjectIdentifier _subject;
        private AuthorizationPermissionType _permission;
        private string _description;

        private GrantPermissionsRequestBuilderImpl() { }

        internal static ISubjectStep Create() => new GrantPermissionsRequestBuilderImpl();

        public IPermissionsStep WithSubject(SubjectIdentifier subject)
        {
            _subject = subject ?? throw new ArgumentNullException(nameof(subject));
            return this;
        }

        public IOptionalStep WithPermission(AuthorizationPermissionType permission)
        {

            _permission = permission;
            return this;
        }

        public IOptionalStep WithDescription(string description)
        {
            _description = description ?? throw new ArgumentNullException(nameof(description));
            return this;
        }

        public GrantAuthorizationPermissionsRequest Build()
        {
            if (_subject is null)
                throw new InvalidOperationException("Metoda WithSubject(...) musi zostać wywołana jako pierwsza.");

            return new GrantAuthorizationPermissionsRequest
            {
                SubjectIdentifier = _subject,
                Permission = _permission,
                Description = _description,
            };
        }
    }
}