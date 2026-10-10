import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FocusTrapDirective } from './focus-trap.directive';

@Component({
  standalone: true,
  imports: [FocusTrapDirective],
  template: `
    <button id="external-trigger">Abrir Modal</button>
    <div appFocusTrap role="dialog" aria-labelledby="modal-title" (escapeKey)="onEscape()">
      <h2 id="modal-title">Título Modal</h2>
      <button id="btn-first">Primeiro</button>
      <input id="input-middle" type="text" />
      <button id="btn-last">Último</button>
    </div>
  `,
})
class TestHostComponent {
  escaped = false;
  onEscape() {
    this.escaped = true;
  }
}

describe('FocusTrapDirective', () => {
  let fixture: ComponentFixture<TestHostComponent>;
  let host: TestHostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    host = fixture.componentInstance;
  });

  it('should trap focus inside dialog and emit escapeKey on Esc', async () => {
    const trigger = fixture.nativeElement.querySelector('#external-trigger') as HTMLElement;
    trigger.focus();
    expect(document.activeElement).toBe(trigger);

    fixture.detectChanges();
    await fixture.whenStable();

    const firstBtn = fixture.nativeElement.querySelector('#btn-first') as HTMLElement;
    const lastBtn = fixture.nativeElement.querySelector('#btn-last') as HTMLElement;

    // Focus enters dialog
    expect(document.activeElement).toBe(firstBtn);

    // Tab on last element wraps to first
    lastBtn.focus();
    const tabEvent = new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true });
    lastBtn.dispatchEvent(tabEvent);
    expect(document.activeElement).toBe(firstBtn);

    // Shift+Tab on first element wraps to last
    const shiftTabEvent = new KeyboardEvent('keydown', { key: 'Tab', shiftKey: true, bubbles: true, cancelable: true });
    firstBtn.dispatchEvent(shiftTabEvent);
    expect(document.activeElement).toBe(lastBtn);

    // Escape emits escapeKey
    const escEvent = new KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true });
    lastBtn.dispatchEvent(escEvent);
    expect(host.escaped).toBe(true);

    // Destroy directive restores focus to trigger
    fixture.destroy();
    expect(document.activeElement).toBe(trigger);
  });
});
