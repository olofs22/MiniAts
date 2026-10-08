import { Application } from '../../../shared/models/application.model';
import { computeDropPosition, toExternalUrl } from './board';

describe('toExternalUrl', () => {
  it('adds https:// to a scheme-less URL', () => {
    expect(toExternalUrl('linkedin.com/in/erik')).toBe('https://linkedin.com/in/erik');
  });

  it('keeps an existing http(s) scheme', () => {
    expect(toExternalUrl('https://www.linkedin.com/in/erik')).toBe('https://www.linkedin.com/in/erik');
    expect(toExternalUrl('HTTP://example.com')).toBe('HTTP://example.com');
  });

  it('trims surrounding whitespace', () => {
    expect(toExternalUrl('  linkedin.com/in/erik ')).toBe('https://linkedin.com/in/erik');
  });
});

function app(position: number): Application {
  return { position } as Application;
}

describe('computeDropPosition', () => {
  it('returns 0 for an empty column', () => {
    expect(computeDropPosition(null, null)).toBe(0);
  });

  it('places a card dropped at the top one below the first card, even if negative', () => {
    expect(computeDropPosition(null, app(0))).toBe(-1);
  });

  it('places a card dropped at the bottom one above the last card', () => {
    expect(computeDropPosition(app(4), null)).toBe(5);
  });

  it('places a card dropped between two cards at their midpoint', () => {
    expect(computeDropPosition(app(1), app(2))).toBe(1.5);
  });

  it('keeps producing a strictly ordered position for many drops into the same gap', () => {
    let after = app(1);
    const before = app(0);
    for (let i = 0; i < 40; i++) {
      const position = computeDropPosition(before, after);
      expect(position).toBeGreaterThan(before.position);
      expect(position).toBeLessThan(after.position);
      after = app(position);
    }
  });
});
