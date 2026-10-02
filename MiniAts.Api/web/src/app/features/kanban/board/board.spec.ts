import { Application } from '../../../shared/models/application.model';
import { computeDropPosition } from './board';

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
