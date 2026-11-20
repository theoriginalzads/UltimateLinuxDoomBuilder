#!/usr/bin/env python3
"""Writes ULDB's ZScript script configuration (Source/ULDB/Assets/Scripting/ZDoom_ZSCRIPT.cfg).

Upstream ships ZDoom_ZSCRIPT.cfg with empty keyword lists ("todo"), so ZScript is barely coloured and
has no autocompletion. This reads the engines' own ZScript (wadsrc/static/zscript of GZDoom and UZDoom)
and lists:
  keywords   - every function and its signature ("Actor.A_Chase(...)"): coloured, offered by
               autocompletion, shown as a call tip and when the pointer rests on the name;
  properties - the language's keywords and built-in types;
  constants  - classes, structs, enum values and constants.

Usage: generate.py OUT.cfg ENGINE_ZSCRIPT_DIR [ENGINE_ZSCRIPT_DIR...]
  e.g. generate.py Source/ULDB/Assets/Scripting/ZDoom_ZSCRIPT.cfg gzdoom/wadsrc/static/zscript uzdoom/wadsrc/static/zscript

Get the sources with a sparse clone:
  git clone --depth 1 --filter=blob:none --sparse https://github.com/ZDoom/gzdoom.git gzdoom
  (cd gzdoom && git sparse-checkout set --no-cone 'wadsrc/static/zscript/')
and the same for https://github.com/UZDoom/UZDoom.git.
"""

import os
import re
import sys

LANGUAGE = """
abstract action alignof array break case class clearscope color const continue default deprecated do else enum
extend fail false final flagdef for foreach function goto if in internal invoker is latent let loop map
mapiterator meta mixin name native null out override play private property protected readonly return sealed
self sizeof sound spriteid state statelabel states static stop string struct super switch textureid transient
true ui unsafe until vararg var version virtual virtualscope void wait while
bool int int8 int16 uint uint8 uint16 float double vector2 vector3 vector4 fvector2 fvector3 fvector4 quat fquat
""".split()

# Functions the compiler provides itself (not declared in any .zs file)
BUILTINS = {
    'random': 'int random[(identifier)](int min = 0, int max = 255)',
    'random2': 'int random2[(identifier)](int mask = 255)',
    'frandom': 'double frandom[(identifier)](double min, double max)',
    'randompick': 'int randompick[(identifier)](int value, ...)',
    'frandompick': 'double frandompick[(identifier)](double value, ...)',
    'abs': 'abs(value)', 'min': 'min(a, b, ...)', 'max': 'max(a, b, ...)', 'clamp': 'clamp(value, min, max)',
    'sqrt': 'double sqrt(double x)', 'exp': 'double exp(double x)', 'log': 'double log(double x)', 'log10': 'double log10(double x)',
    'ceil': 'double ceil(double x)', 'floor': 'double floor(double x)', 'round': 'double round(double x)',
    'sin': 'double sin(double degrees)', 'cos': 'double cos(double degrees)', 'tan': 'double tan(double degrees)',
    'asin': 'double asin(double x)', 'acos': 'double acos(double x)', 'atan': 'double atan(double x)',
    'atan2': 'double atan2(double y, double x)', 'sinh': 'double sinh(double x)', 'cosh': 'double cosh(double x)', 'tanh': 'double tanh(double x)',
    'VectorAngle': 'double VectorAngle(double x, double y)',
    'GetDefaultByType': 'readonly<Actor> GetDefaultByType(class<Actor> type)',
}

MODIFIERS = set("""
native static virtual override final abstract action protected private public clearscope play ui const
vararg readonly transient internal latent sealed meta virtualscope unsafe
""".split())

TOKEN = re.compile(r'"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|[A-Za-z_][A-Za-z0-9_]*|\d+(?:\.\d*)?(?:[eE][-+]?\d+)?|::|\S')


def strip_comments(text):
    out, i, n = [], 0, len(text)
    while i < n:
        c = text[i]
        if c == '"' or c == "'":
            j = i + 1
            while j < n and text[j] != c and text[j] != '\n':
                j += 2 if text[j] == '\\' else 1
            out.append(text[i:j + 1]); i = j + 1
        elif text.startswith('//', i):
            j = text.find('\n', i); i = n if j < 0 else j
        elif text.startswith('/*', i):
            j = text.find('*/', i + 2); i = n if j < 0 else j + 2
            out.append(' ')
        else:
            out.append(c); i += 1
    return ''.join(out)


def skip_block(tokens, i):
    """tokens[i] is '{': returns the index after the matching '}'."""
    depth = 0
    while i < len(tokens):
        if tokens[i] == '{': depth += 1
        elif tokens[i] == '}':
            depth -= 1
            if depth == 0: return i + 1
        i += 1
    return i


def statement(tokens, i):
    """From i to the next ';' or '{' at bracket depth 0: (tokens, terminator, index after it)."""
    start, depth = i, 0
    while i < len(tokens):
        t = tokens[i]
        if t in '([': depth += 1
        elif t in ')]': depth -= 1
        elif depth <= 0 and t in (';', '{', '}'):
            return tokens[start:i], t, i + 1
        i += 1
    return tokens[start:i], None, i


def join(tokens):
    s = ' '.join(tokens)
    s = re.sub(r'\s*([(),\[\]<.])\s*', r'\1', s)
    s = re.sub(r'\s*>', '>', s)
    s = re.sub(r',', ', ', s)
    s = re.sub(r'\s*=\s*', ' = ', s)
    return s


class Collector:
    def __init__(self):
        self.functions = {}   # name -> {signature}
        self.types = set()     # classes and structs
        self.constants = set()

    def add_function(self, owner, stmt):
        p = stmt.index('(')
        name = stmt[p - 1] if p > 0 else None
        if not name or not re.match(r'[A-Za-z_]\w*$', name) or name.lower() in LANGUAGE:
            return
        prefix = [t for t in stmt[:p - 1] if t not in MODIFIERS]
        # deprecated("x.y"), version("x.y") are modifiers with arguments
        cleaned, k = [], 0
        while k < len(prefix):
            if prefix[k] in ('deprecated', 'version') and k + 1 < len(prefix) and prefix[k + 1] == '(':
                k += 2
                while k < len(prefix) and prefix[k] != ')': k += 1
                k += 1
                continue
            cleaned.append(prefix[k]); k += 1
        if not cleaned:
            return  # a call, not a declaration
        depth, q = 0, p
        while q < len(stmt):
            if stmt[q] == '(': depth += 1
            elif stmt[q] == ')':
                depth -= 1
                if depth == 0: break
            q += 1
        params = join(stmt[p + 1:q])
        rettype = join(cleaned)
        sig = (rettype + ' ' if rettype else '') + (owner + '.' if owner else '') + name + '(' + params + ')'
        self.functions.setdefault(name, set()).add(sig)

    def enum_body(self, tokens, i):
        """tokens[i] is '{' of an enum: collects the names, returns the index after '}'."""
        end = skip_block(tokens, i)
        expect = True
        depth = 0
        for t in tokens[i + 1:end - 1]:
            if t in '([': depth += 1
            elif t in ')]': depth -= 1
            elif depth == 0 and t == ',':
                expect = True; continue
            if expect and re.match(r'[A-Za-z_]\w*$', t):
                self.constants.add(t)
            expect = False
        return end

    def body(self, tokens, i, owner):
        """Members of a class or struct; tokens[i] is just after its '{'. Returns the index after '}'."""
        while i < len(tokens):
            if tokens[i] == '}':
                return i + 1
            stmt, term, nxt = statement(tokens, i)
            low = [t.lower() for t in stmt]
            if term == '{':
                if low[:1] == ['enum']:
                    i = self.enum_body(tokens, nxt - 1); continue
                if low[:1] in (['struct'], ['class']) and len(stmt) > 1:
                    self.types.add(stmt[1])
                    i = self.body(tokens, nxt, stmt[1]); continue
                if '(' in stmt:
                    self.add_function(owner, stmt)
                i = skip_block(tokens, nxt - 1)  # function body, States, Default
                continue
            if term == ';' and stmt:
                if '(' in stmt and low[0] not in ('property', 'flagdef', 'mixin', 'enum'):
                    self.add_function(owner, stmt)
                elif low[0] == 'const' and len(stmt) > 1:
                    self.constants.add(stmt[1])
                elif low[0] == 'static' and len(low) > 1 and low[1] == 'const' and len(stmt) > 3:
                    # static const type name[] = {...};
                    names = [t for t in stmt if re.match(r'[A-Za-z_]\w*$', t)]
                    if len(names) >= 4: self.constants.add(names[3])
            if term is None:
                return nxt
            i = nxt
        return i

    def file(self, text):
        tokens = TOKEN.findall(strip_comments(text))
        i = 0
        while i < len(tokens):
            stmt, term, nxt = statement(tokens, i)
            low = [t.lower() for t in stmt]
            if term == '{':
                if low[:1] == ['enum']:
                    i = self.enum_body(tokens, nxt - 1); continue
                kind = None
                for k, t in enumerate(low):
                    if t in ('class', 'struct'):
                        kind = k; break
                if kind is not None and kind + 1 < len(stmt):
                    name = stmt[kind + 1]
                    if 'extend' not in low[:kind]: self.types.add(name)
                    i = self.body(tokens, nxt, name); continue
                i = skip_block(tokens, nxt - 1); continue
            if term == ';' and low[:1] == ['const'] and len(stmt) > 1:
                self.constants.add(stmt[1])
            if term is None: break
            i = nxt


def cfg_string(s):
    return s.replace('\\', '\\\\').replace('"', '\\"').replace('\n', '\\n')


def main():
    if len(sys.argv) < 3:
        print(__doc__); sys.exit(1)
    out, dirs = sys.argv[1], sys.argv[2:]
    c = Collector()
    count = 0
    for d in dirs:
        for root, _, files in os.walk(d):
            for f in sorted(files):
                if f.endswith('.zs') or f.endswith('.txt') and f.startswith('zscript'):
                    with open(os.path.join(root, f), encoding='utf-8', errors='replace') as fh:
                        c.file(fh.read())
                    count += 1

    language = set(w.lower() for w in LANGUAGE)
    for name, sig in BUILTINS.items():
        c.functions.setdefault(name, set()).add(sig)
    keywords = {}
    for name, sigs in c.functions.items():
        if name.lower() in language: continue
        sigs = sorted(sigs)
        if len(sigs) > 4:
            sigs = sigs[:4] + ['... (' + str(len(sigs) - 4) + ' more)']
        keywords[name] = '\n'.join(sigs)
    # A name can be a function and a class (or a constant); the function list wins, so it's coloured once
    lower_keywords = set(k.lower() for k in keywords)
    constants = sorted((c.types | c.constants) - set(keywords), key=str.lower)
    constants = [k for k in constants if k.lower() not in lower_keywords and k.lower() not in language]

    lines = []
    lines.append('/*******************************************************************\\')
    lines.append('\tULDB script highlighting definitions for ZSCRIPT')
    lines.append('\tGenerated by tools/zscriptcfg/generate.py from the ZScript of GZDoom and UZDoom')
    lines.append('\t(' + str(count) + ' files). Do not edit by hand: run the generator again.')
    lines.append('\\*******************************************************************/')
    lines.append('')
    lines.append('// Editor settings')
    lines.append('description = "ZDoom ZSCRIPT";')
    lines.append('codepage = 0;')
    lines.append('extensions = "zs,zsc,txt";')
    lines.append('casesensitive = false;')
    lines.append('insertcase = 0;\t// 0=Normal, 1=Lowercase, 2=Uppercase')
    lines.append('lexer = 35;\t// CPP-style, case-insensitive')
    lines.append('functionopen = "(";')
    lines.append('functionclose = ")";')
    lines.append('codeblockopen = "{";')
    lines.append('codeblockclose = "}";')
    lines.append('arrayopen = "[";')
    lines.append('arrayclose = "]";')
    lines.append('argumentdelimiter = ",";')
    lines.append('terminator = ";";')
    lines.append('keywordhelp = "https://zdoom.org/wiki/%K";')
    lines.append('scripttype = "ZSCRIPT";')
    lines.append('')
    lines.append('// Functions, with their signatures (class.name)')
    lines.append('keywords')
    lines.append('{')
    for k in sorted(keywords, key=str.lower):
        lines.append('\t' + k + ' = "' + cfg_string(keywords[k]) + '";')
    lines.append('}')
    lines.append('')
    lines.append('// The language\'s keywords and built-in types')
    lines.append('properties')
    lines.append('{')
    for k in sorted(set(LANGUAGE), key=str.lower):
        lines.append('\t' + k + ';')
    lines.append('}')
    lines.append('')
    lines.append('// Classes, structs, enum values and constants')
    lines.append('constants')
    lines.append('{')
    for k in constants:
        lines.append('\t' + k + ';')
    lines.append('}')
    os.makedirs(os.path.dirname(out) or '.', exist_ok=True)
    with open(out, 'w', newline='\n') as fh:
        fh.write('\n'.join(lines) + '\n')
    print(out + ': ' + str(len(keywords)) + ' functions, ' + str(len(set(LANGUAGE))) + ' language keywords, ' + str(len(constants)) + ' constants from ' + str(count) + ' files')


if __name__ == '__main__':
    main()
