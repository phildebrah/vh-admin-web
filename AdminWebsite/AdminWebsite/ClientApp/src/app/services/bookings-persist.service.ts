import { Injectable } from '@angular/core';
import { BookingsListModel, BookingsDetailsModel } from '../common/model/bookings-list.model';
import { HearingModel } from '../common/model/hearing.model';
import { ParticipantModel } from '../common/model/participant.model';

@Injectable({ providedIn: 'root' })
export class BookingPersistService {
    private _bookingList: Array<BookingsListModel> = [];
    private _nextCursor: string;
    private _selectedGroupIndex: number;
    private _selectedItemIndex: number;
    private _caseNumber: string;
    private _selectedVenueIds: number[] = [];
    private _selectedCaseTypes: string[] = [];
    private _startDate: Date;
    private _endDate: Date;
    private _participantLastName: string;
    private _noJudgeInHearings: boolean;
    private _showSearch = false;
    private readonly SelectedHearingIdKey = 'SelectedHearingIdKey';

    resetAll() {
        this._bookingList = [];
        this._nextCursor = undefined;
        this.selectedGroupIndex = -1;
        this.selectedItemIndex = -1;
        sessionStorage.removeItem(this.SelectedHearingIdKey);
    }

    updateBooking(hearing: HearingModel): BookingsDetailsModel {
        if (
            this._bookingList.length > this._selectedGroupIndex &&
            this._bookingList[this._selectedGroupIndex].BookingsDetails.length > this._selectedItemIndex
        ) {
            const hearingUpdate = this._bookingList[this._selectedGroupIndex].BookingsDetails[this.selectedItemIndex];
            if (hearingUpdate.HearingId === hearing.hearing_id) {
                const newStartDate = new Date(hearing.scheduled_date_time);

                hearingUpdate.IsStartTimeChanged = hearingUpdate.StartTime.toString() !== newStartDate.toString();
                hearingUpdate.Selected = true;

                hearingUpdate.HearingCaseName = hearing.cases && hearing.cases.length > 0 ? hearing.cases[0].name : '';
                hearingUpdate.HearingCaseNumber = hearing.cases && hearing.cases.length > 0 ? hearing.cases[0].number : '';
                hearingUpdate.StartTime = newStartDate;
                hearingUpdate.Duration = hearing.scheduled_duration;
                hearingUpdate.CourtRoomAccount = hearing.participants.find(x => x.is_judge)?.username;
                hearingUpdate.CourtAddress = hearing.court_name;
                hearingUpdate.CourtRoom = hearing.court_room;
                hearingUpdate.CreatedBy = hearing.created_by;
                hearingUpdate.Status = hearing.status;
                hearingUpdate.TelephoneConferenceId = hearing.telephone_conference_id;
                if (this.isValidDate(hearing.created_date)) {
                    hearingUpdate.CreatedDate = new Date(hearing.created_date);
                }
                hearingUpdate.LastEditBy = hearing.updated_by;

                if (this.isValidDate(hearing.updated_date)) {
                    hearingUpdate.LastEditDate = new Date(hearing.updated_date);
                }
                hearingUpdate.JudgeName = this.getJudgeName(hearing.participants);
                return hearingUpdate;
            }
        }
    }

    isValidDate(value: any): boolean {
        if (value) {
            const timestamp = Date.parse(value.toString());
            return isNaN(timestamp) === false;
        }
        return false;
    }

    getJudgeName(participants: ParticipantModel[]) {
        const judge = participants.find(x => x.case_role_name === 'Judge');
        return judge ? judge.display_name : '';
    }

    set bookingList(value: Array<BookingsListModel>) {
        this._bookingList = value;
    }

    get bookingList(): Array<BookingsListModel> {
        return this._bookingList;
    }

    set nextCursor(value: string) {
        this._nextCursor = value;
    }

    get nextCursor(): string {
        return this._nextCursor;
    }

    get caseNumber(): string {
        return this._caseNumber;
    }

    set caseNumber(value) {
        this._caseNumber = value;
    }

    get participantLastName(): string {
        return this._participantLastName;
    }

    set participantLastName(value) {
        this._participantLastName = value;
    }

    get noJugdeInHearings(): boolean {
        return this._noJudgeInHearings;
    }

    set noJugdeInHearings(value) {
        this._noJudgeInHearings = value;
    }

    get selectedVenueIds(): number[] {
        return this._selectedVenueIds;
    }

    set selectedVenueIds(value) {
        this._selectedVenueIds = value;
    }

    get selectedCaseTypes(): string[] {
        return this._selectedCaseTypes;
    }

    set selectedCaseTypes(value) {
        this._selectedCaseTypes = value;
    }

    get startDate(): Date {
        return this._startDate;
    }

    set startDate(value) {
        this._startDate = value ? new Date(value) : null;
    }

    get endDate(): Date {
        return this._endDate;
    }

    set endDate(value) {
        this._endDate = value ? new Date(value) : null;
    }

    set selectedGroupIndex(value: number) {
        this._selectedGroupIndex = value;
    }

    get selectedGroupIndex() {
        return this._selectedGroupIndex;
    }

    set selectedItemIndex(value: number) {
        this._selectedItemIndex = value;
    }

    get selectedItemIndex() {
        return this._selectedItemIndex;
    }

    set selectedHearingId(value: string) {
        sessionStorage.setItem(this.SelectedHearingIdKey, value);
    }

    get selectedHearingId() {
        return sessionStorage.getItem(this.SelectedHearingIdKey);
    }

    get showSearch(): boolean {
        return this._showSearch;
    }

    set showSearch(value) {
        this._showSearch = value;
    }
}
