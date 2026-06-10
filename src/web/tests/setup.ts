import '@testing-library/jest-dom';

// jsdom does not implement ResizeObserver; cmdk (used by CommandPalette) requires it
global.ResizeObserver = class ResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
};
