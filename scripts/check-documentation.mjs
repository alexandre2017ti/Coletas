import { execFileSync } from 'node:child_process'
import { existsSync, readFileSync } from 'node:fs'

// Motivo: exigir rastreabilidade antes de integrar código ao repositório.
// Mudança: docs/mudancas/2026-09-09-02-fase-zero.md
const base = process.argv[2]
const git = (...args) => execFileSync('git', args, { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] }).trim()
let hasHead = true
try { git('rev-parse', '--verify', 'HEAD') } catch { hasHead = false }
const changed = base
  ? git('diff', '--name-only', base, 'HEAD').split('\n')
  : [...(hasHead ? git('diff', '--name-only', 'HEAD').split('\n') : []), ...git('ls-files', '--others', '--exclude-standard').split('\n')]
const added = base
  ? git('diff', '--name-only', '--diff-filter=A', base, 'HEAD').split('\n')
  : [...(hasHead ? git('diff', '--name-only', '--diff-filter=A', 'HEAD').split('\n') : git('ls-files', '--cached').split('\n')), ...git('ls-files', '--others', '--exclude-standard').split('\n')]
const records = added.filter(p => /^docs\/mudancas\/\d{4}-\d{2}-\d{2}-\d{2}-.+\.md$/.test(p))
if (changed.some(Boolean) && records.length === 0) throw new Error('Toda mudança exige registro em docs/mudancas/.')
for (const file of records) {
  if (!existsSync(file)) continue
  const content = readFileSync(file, 'utf8').toLowerCase()
  for (const term of ['motivo', 'escopo', 'validação', 'rollback']) {
    if (!content.includes(term)) throw new Error(file + ': seção ausente: ' + term)
  }
}
console.log('Documentação da mudança verificada. Revisão humana do motivo no código continua obrigatória.')
