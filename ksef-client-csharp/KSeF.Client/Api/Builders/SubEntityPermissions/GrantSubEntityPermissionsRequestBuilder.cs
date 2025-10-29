using KSeF.Client.Core.Models.Permissions.SubUnit;

namespace KSeF.Client.Api.Builders.SubUnitPermissions;

public static class GrantSubUnitPermissionsRequestBuilder
{
    public static ISubjectStep Create() => GrantPermissionsRequestBuilderImpl.Create();

    public interface ISubjectStep
    {
        IContextStep WithSubject(SubjectIdentifier subject);
    }

    public interface IContextStep
    {
        IOptionalStep WithContext(ContextIdentifier context);
    }

    public interface ISubunitNameStep
    {
        IOptionalStep WithSubunitName(string subunitName);
    }

    public interface IOptionalStep
    {
        IOptionalStep WithDescription(string description);
        IOptionalStep WithSubunitName(string subunitName);
        GrantPermissionsSubUnitRequest Build();
    }

    private sealed class GrantPermissionsRequestBuilderImpl :
        ISubjectStep,
        IContextStep,
        IOptionalStep
    {
        private SubjectIdentifier _subject;
        private ContextIdentifier _context;
        private string _description;
        private string _subunitName;

        private GrantPermissionsRequestBuilderImpl() { }

        internal static ISubjectStep Create() => new GrantPermissionsRequestBuilderImpl();

        public IContextStep WithSubject(SubjectIdentifier subject)
        {
            _subject = subject ?? throw new ArgumentNullException(nameof(subject));
            return this;
        }

        public IOptionalStep WithContext(ContextIdentifier context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            return this;
        }

        public IOptionalStep WithDescription(string description)
        {
            _description = description ?? throw new ArgumentNullException(nameof(description));
            return this;
        }

        public IOptionalStep WithSubunitName(string subunitName)
        {
            _subunitName = subunitName;
            return this;
        }

        public GrantPermissionsSubUnitRequest Build()
        {
            if (_subject is null)
                throw new InvalidOperationException("Metoda WithSubject(...) musi zostać wywołana jako pierwsza.");
            if (_context is null)
                throw new InvalidOperationException("Metoda WithContext(...) musi zostać wywołana po ustawieniu podmiotu.");
            if (_context.Type == ContextIdentifierType.InternalId && string.IsNullOrWhiteSpace(_subunitName))
                throw new InvalidOperationException("Dla typu ContextIdentifierType.InternalId, metoda WithSubunitName(...) musi zostać wywołana przed Build().");

            return new GrantPermissionsSubUnitRequest
            {
                SubjectIdentifier = _subject,
                ContextIdentifier = _context,
                Description = _description,
                SubunitName = _subunitName
            };
        }
    }
}