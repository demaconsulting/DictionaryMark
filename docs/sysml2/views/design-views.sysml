// Views rendered into docs/design/generated/ for the Design document.
// Per SysML2Tools syntax, 'expose' is only valid inside a named 'view' usage
// (not a 'view def' definition), and it - not 'render' - is what scopes the
// rendered diagram content.
package DictionaryMarkDesignViews {
    private import StandardViewDefinitions::*;
    private import DictionaryMark::*;

    view SoftwareStructureView : InterconnectionView {
        expose DictionaryMark::DictionaryMarkSystem;
    }

    view DictionaryMarkView {
        expose DictionaryMarkSystem;
    }

    view CliView {
        expose CliSubsystem;
    }

    view DictionaryView {
        expose DictionarySubsystem;
    }

    view SelfTestView {
        expose SelfTestSubsystem;
    }

    view UtilitiesView {
        expose UtilitiesSubsystem;
    }

    view FilesView {
        expose FilesSubsystem;
    }
}
