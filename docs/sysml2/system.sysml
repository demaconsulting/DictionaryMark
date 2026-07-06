// Stable, template-controlled entry point into this repository's SysML2 model.
// This filename is identical across all DEMA Consulting tools so that AI agents
// and CI tooling can query "System" without needing to know the project's name.
//
// Only the "specializes" target below is project-specific.

import DictionaryMark::*;

part def System :> DictionaryMarkSystem {
    doc /* Alias for the DictionaryMark system, stable across all DEMA Consulting tools. */
}
