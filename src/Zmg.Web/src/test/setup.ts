// Minimal localStorage for the pure-module tests. The suite runs in the `node` environment (jsdom
// pulls an ESM-only CSS dep that breaks under Node < 22.12), so we install a Map-backed Storage —
// enough for readPersisted/writePersisted, and a real prototype so `vi.spyOn` can force throws.
class MemoryStorage implements Storage {
  private store = new Map<string, string>();

  get length(): number {
    return this.store.size;
  }

  clear(): void {
    this.store.clear();
  }

  getItem(key: string): string | null {
    return this.store.has(key) ? this.store.get(key)! : null;
  }

  key(index: number): string | null {
    return [...this.store.keys()][index] ?? null;
  }

  removeItem(key: string): void {
    this.store.delete(key);
  }

  setItem(key: string, value: string): void {
    this.store.set(key, String(value));
  }
}

globalThis.Storage = MemoryStorage as unknown as typeof Storage;
globalThis.localStorage = new MemoryStorage();
