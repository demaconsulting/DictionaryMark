// Views rendered into docs/design/generated/ for the Design document.
// Reopens the DictionaryMark package so each 'expose' statement can reference
// its target part def directly. Per SysML2Tools syntax, 'expose' is only valid
// inside a named 'view' usage (not a 'view def' definition), and it - not
// 'render' - is what scopes the rendered diagram content.
package DictionaryMark {
    view SoftwareStructureView {
        expose DictionaryMark;
        render asTreeDiagram;
    }

    view DictionaryMarkView {
        expose DictionaryMarkSystem;
        render asTreeDiagram;
    }

    view CliView {
        expose CliSubsystem;
        render asTreeDiagram;
    }

    view DictionaryView {
        expose DictionarySubsystem;
        render asTreeDiagram;
    }

    view SelfTestView {
        expose SelfTestSubsystem;
        render asTreeDiagram;
    }

    view UtilitiesView {
        expose UtilitiesSubsystem;
        render asTreeDiagram;
    }
}
