# Roadmap

## Phase 9B - True Shape Nesting / NFP Foundation (Current)
- [x] True Shape Nesting algorithm ✅ (2026-06-13)
- [x] NFP-based candidate generation (vertex/edge contacts)
- [x] Multi-criteria scoring (Y, X, compactness, edge contact)
- [x] Small-part gap fill pass ✅ (2026-06-13)
- [x] SAT polygon collision ✅ (2026-06-13)
- [x] Timeout/fallback to Free Rectangle ✅ (2026-06-13)
- [x] Unit tests (8 new tests) ✅ (2026-06-13)
- [x] TrueShape debug counters + gap sample probing ✅ (2026-06-13)
- [x] TrueShape 9B.2 plate-offset overlay fix + dense free-space sampling ✅ (2026-06-14)

## Phase 9A - CAD Workspace Professionalization & Shape-Aware Nesting (Completed)
- [x] Workspace visual polish - grid, rulers, plate borders
- [x] Part display quality improvements
- [x] Shape-Aware Nesting review document
- [x] Shape-Aware Polygon algorithm ✅ (2026-06-13)
- [x] Vertex-based candidate placement ✅ (2026-06-13)

## Phase 8 - Irregular Shape Optimization
- [x] FAZ 8A - SAT Collision System
- [x] FAZ 8B - Geometry Based Placement & Candidate Generation
- [x] FAZ 8C - Production DXF Export & Reporting
- [x] FAZ 8D - Project System (.nelp Support)
- [x] FAZ 8E - Operation Manager & Laser Process Pipeline
- [x] FAZ 8J - PDF Quotation & Production Report System
- [x] FAZ 8O - CAD Workspace Performance & Measurement UX
- [ ] FAZ 8F - No-Fit Polygon (NFP) Implementation (Deferred)

## Phase 10 - UI & UX Improvements
- [ ] Gelişmiş parça listesi ve önizleme.
- [ ] Manuel sürükle-bırak yerleşim düzenleme.
- [ ] Katman bazlı önceliklendirme.
- [ ] Operations sekmesi iyileştirmeleri (grup bazlı filtreleme, batch işlemler).

## Phase 11 - Export & Integration
- [ ] G-Code üretimi.
- [ ] Diğer CAD formatları desteği (SVG, AI).
- [ ] Lazer markalama/kazıma için güç-hız profili export.

---

## FAZ 9A & 9B Completed (2026-06-13)

**FAZ 9A:**
- Workspace visual polish: improved grid contrast, ruler visibility, plate borders, selection overlays
- Part display quality: 2px stroke thickness, better colors (#4EC9B0), selection visibility
- Created docs/SHAPE_AWARE_NESTING_REVIEW.md with nesting algorithm analysis
- Fixed viewport culling bug causing parts to disappear
- Portable build fixed: self-contained, no .NET runtime required
- Shape-Aware Polygon nesting algorithm: vertex-based anchor points, 15s timeout, Free Rectangle fallback
- 5 new tests in NestingAlgorithmTests.cs

**FAZ 9B - True Shape Nesting:**
- TrueShapeNesting algorithm using NFP-based candidate generation
- Vertex-to-vertex, vertex-to-edge, edge-to-edge contact candidates
- Multi-criteria scoring (Y, X, compactness, edge contact proximity)
- Small-part gap fill pass for filling actual polygon gaps
- SAT polygon collision for exact placement validation
- 15s timeout with Free Rectangle fallback
- 8 new tests in TrueShapeNestingTests.cs
- 41 total tests passing
