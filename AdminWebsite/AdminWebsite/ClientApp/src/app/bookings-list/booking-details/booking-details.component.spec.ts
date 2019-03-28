import { ComponentFixture, TestBed, async } from '@angular/core/testing';
import { HttpClientModule } from '@angular/common/http';
import { Router } from '@angular/router';

import { Component, Input } from '@angular/core';
import { BookingDetailsComponent } from './booking-details.component';
import { VideoHearingsService } from '../../services/video-hearings.service';
import { BookingDetailsService } from '../../services/booking-details.service';
import { BookingService } from '../../services/booking.service';
import { HearingDetailsResponse } from '../../services/clients/api-client';
import { BookingsDetailsModel } from '../../common/model/bookings-list.model';
import { ParticipantDetailsModel } from '../../common/model/participant-details.model';
import { of } from 'rxjs';
import { HearingModel } from '../../common/model/hearing.model';
import { CaseModel } from '../../common/model/case.model';
import { PageUrls } from '../../shared/page-url.constants';
import { CancelBookingPopupComponent } from 'src/app/popups/cancel-booking-popup/cancel-booking-popup.component';

let component: BookingDetailsComponent;
let fixture: ComponentFixture<BookingDetailsComponent>;
let videoHearingServiceSpy: jasmine.SpyObj<VideoHearingsService>;
let routerSpy: jasmine.SpyObj<Router>;
let bookingServiceSpy: jasmine.SpyObj<BookingService>;

export class BookingDetailsTestData {
  getBookingsDetailsModel() {
    return new BookingsDetailsModel('44', new Date('2019-11-22 13:58:40.3730067'),
      120, 'XX3456234565', 'Smith vs Donner', 'Tax', '', '33A', 'Coronation Street',
      'John Smith', new Date('2018-10-22 13:58:40.3730067'), 'Roy Ben', new Date('2018-10-22 13:58:40.3730067'), 'Booked');
  }

  getParticipants() {
    const participants: Array<ParticipantDetailsModel> = [];
    const judges: Array<ParticipantDetailsModel> = [];
    const p1 = new ParticipantDetailsModel('1', 'Mrs', 'Alan', 'Brake', 'Judge', 'email.p1@email.com',
      'email1@co.uk', 'Claimant', 'Solicitor', 'Alan Brake', '');
    const p2 = new ParticipantDetailsModel('2', 'Mrs', 'Roy', 'Bark', 'Citizen', 'email.p2@email.com',
      'email2@co.uk', 'Claimant', 'Claimant LIP', 'Roy Bark', '');
    const p3 = new ParticipantDetailsModel('2', 'Mrs', 'Fill', 'Green', 'Professional', 'email.p3@email.com',
      'email3@co.uk', 'Defendant', 'Defendant LIP', 'Fill', '');
    participants.push(p2);
    participants.push(p3);
    judges.push(p1);
    return { judges: judges, participants: participants };
  }
}

@Component({
  selector: 'app-booking-participant-list',
  template: ''
})
class BookingParticipantListMockComponent {
  @Input()
  participants: Array<ParticipantDetailsModel> = [];

  @Input()
  judges: Array<ParticipantDetailsModel> = [];

  @Input()
  vh_officer_admin: boolean;
}

@Component({
  selector: 'app-hearing-details',
  template: ''
})
class HearingDetailsMockComponent {
  @Input()
  hearing: BookingsDetailsModel;
}

const hearingResponse = new HearingDetailsResponse();

const caseModel = new CaseModel();
caseModel.name = 'X vs Y';
caseModel.number = 'XX3456234565';
const hearingModel = new HearingModel();
hearingModel.hearing_id = '44';
hearingModel.cases = [caseModel];
hearingModel.scheduled_duration = 120;

class BookingDetailsServiceMock {
  mapBooking(response) {
    return new BookingDetailsTestData().getBookingsDetailsModel();
  }
  mapBookingParticipants(response) {
    return new BookingDetailsTestData().getParticipants();
  }
}

describe('BookingDetailsComponent', () => {
  videoHearingServiceSpy = jasmine.createSpyObj('VodeoHearingService',
    ['getHearingById', 'saveHearing', 'mapHearingDetailsResponseToHearingModel', 'updateHearingRequest']);
  routerSpy = jasmine.createSpyObj('Router', ['navigate']);
  bookingServiceSpy = jasmine.createSpyObj('BookingService', ['setEditMode',
    'resetEditMode', 'setExistingCaseType', 'removeExistingCaseType']);

  beforeEach(async(() => {
    videoHearingServiceSpy.getHearingById.and.returnValue(of(hearingResponse));
    videoHearingServiceSpy.mapHearingDetailsResponseToHearingModel.and.returnValue(hearingModel);

    TestBed.configureTestingModule({
      declarations: [
        BookingDetailsComponent,
        BookingParticipantListMockComponent,
        HearingDetailsMockComponent,
        CancelBookingPopupComponent
      ],
      imports: [HttpClientModule],
      providers: [{ provide: VideoHearingsService, useValue: videoHearingServiceSpy },
      { provide: BookingDetailsService, useClass: BookingDetailsServiceMock },
      { provide: Router, useValue: routerSpy },
      { provide: BookingService, useValue: bookingServiceSpy }
      ]
    }).compileComponents();
    fixture = TestBed.createComponent(BookingDetailsComponent);
    component = fixture.componentInstance;
    component.hearingId = '1';
    fixture.detectChanges();
  }));

  it('should create component', (() => {
    expect(component).toBeTruthy();
  }));

  it('should get hearings details', (() => {
    component.ngOnInit();
    expect(videoHearingServiceSpy.getHearingById).toHaveBeenCalled();
    expect(component.hearing).toBeTruthy();
    expect(component.hearing.HearingId).toBe('44');
    expect(component.hearing.Duration).toBe(120);
    expect(component.hearing.HearingCaseNumber).toBe('XX3456234565');
  }));
  it('should get hearings details and map to HearingModel', (() => {
    component.ngOnInit();
    expect(videoHearingServiceSpy.mapHearingDetailsResponseToHearingModel).toHaveBeenCalled();
    expect(component.booking).toBeTruthy();
    expect(component.booking.hearing_id).toBe('44');
    expect(component.booking.scheduled_duration).toBe(120);
    expect(component.booking.cases[0].number).toBe('XX3456234565');
  }));

  it('should get judge details', (() => {
    component.ngOnInit();
    expect(component.judges).toBeTruthy();
    expect(component.judges.length).toBe(1);
    expect(component.judges[0].UserRoleName).toBe('Judge');
    expect(component.judges[0].ParticipantId).toBe('1');
    expect(component.judges[0].FirstName).toBe('Alan');
  }));

  it('should get participants details', (() => {
    component.ngOnInit();
    expect(component.participants).toBeTruthy();
    expect(component.participants.length).toBe(2);
    expect(component.participants[0].UserRoleName).toBe('Citizen');
    expect(component.participants[0].ParticipantId).toBe('2');
  }));
  it('should set edit mode if the edit button pressed', () => {
    component.editHearing();
    expect(videoHearingServiceSpy.updateHearingRequest).toHaveBeenCalled();
    expect(bookingServiceSpy.resetEditMode).toHaveBeenCalled();
    expect(routerSpy.navigate).toHaveBeenCalledWith([PageUrls.Summary]);
  });
});

